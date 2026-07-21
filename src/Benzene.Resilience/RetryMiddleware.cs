using Benzene.Abstractions.Middleware;

namespace Benzene.Resilience;

/// <summary>
/// Non-generic companion to <see cref="RetryMiddleware{TContext}"/> holding static helpers that
/// don't need a <c>TContext</c> type argument (matching the <see cref="Task"/>/<see cref="Task{T}"/>
/// pattern).
/// </summary>
public static class RetryMiddleware
{
    /// <summary>
    /// The "full jitter" backoff algorithm (AWS's documented recommendation): returns a random
    /// duration between zero and the input delay. Pass this as the <c>jitter</c>
    /// constructor/<c>UseRetry</c> parameter to spread out retries from many callers that backed off
    /// at the same moment, instead of them all retrying in lockstep (the "thundering herd" problem
    /// plain exponential backoff doesn't address on its own).
    /// </summary>
    /// <param name="random">The random source to use. Defaults to <see cref="Random.Shared"/>.</param>
    public static Func<TimeSpan, TimeSpan> FullJitter(Random? random = null)
    {
        var rng = random ?? Random.Shared;
        return delay => TimeSpan.FromMilliseconds(rng.NextDouble() * delay.TotalMilliseconds);
    }
}

public class RetryMiddleware<TContext> : IMiddleware<TContext>
{
    private readonly int _numberOfRetries;
    private readonly TimeSpan _initialDelay;
    private readonly double _backoffFactor;
    private readonly TimeSpan? _maxDelay;
    private readonly Func<Exception, bool> _shouldRetry;
    private readonly Func<TContext, bool> _shouldRetryContext;
    private readonly Func<TimeSpan, TimeSpan> _jitter;
    private readonly Func<TimeSpan, Task> _delay;

    // Task.Delay rejects a delay above int.MaxValue milliseconds (~24.8 days). With no maxDelay set
    // the uncapped exponential sleep crosses that ceiling around attempt ~25, so the actual sleep is
    // clamped here. This is distinct from the TimeSpan.FromMilliseconds overflow clamp below, which
    // only guards the much larger TimeSpan.MaxValue boundary used to grow the next attempt's delay.
    private static readonly TimeSpan MaxSleep = TimeSpan.FromMilliseconds(int.MaxValue);

    public RetryMiddleware(
        int numberOfRetries = 3,
        TimeSpan? initialDelay = null,
        double backoffFactor = 2.0,
        TimeSpan? maxDelay = null,
        Func<Exception, bool>? shouldRetry = null,
        Func<TContext, bool>? shouldRetryContext = null,
        Func<TimeSpan, TimeSpan>? jitter = null,
        Func<TimeSpan, Task>? delay = null)
    {
        _numberOfRetries = numberOfRetries;
        _initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(200);
        _backoffFactor = backoffFactor;
        _maxDelay = maxDelay;
        _shouldRetry = shouldRetry ?? DefaultShouldRetry;
        _shouldRetryContext = shouldRetryContext ?? DefaultShouldRetryContext;
        _jitter = jitter ?? NoJitter;
        _delay = delay ?? Task.Delay;
    }

    public string Name => nameof(RetryMiddleware<TContext>);

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var attempt = 0;
        var delay = _initialDelay;
        while (true)
        {
            try
            {
                await next();

                if (attempt >= _numberOfRetries || !_shouldRetryContext(context))
                {
                    return;
                }
            }
            catch (Exception ex) when (attempt < _numberOfRetries && _shouldRetry(ex))
            {
            }

            attempt++;

            // The max-delay cap and jitter apply only to the actual sleep - the exponential growth
            // driving `delay` itself is left uncapped/unjittered, so later attempts still compound
            // off the true exponential curve (matching AWS's documented "full jitter" algorithm:
            // sleep = random(0, min(cap, base * factor^attempt))).
            var cappedDelay = _maxDelay.HasValue && delay > _maxDelay.Value ? _maxDelay.Value : delay;
            var sleep = _jitter(cappedDelay);
            await _delay(sleep > MaxSleep ? MaxSleep : sleep);
            // Grow the exponential curve, but clamp at TimeSpan.MaxValue: with a large numberOfRetries
            // (or initialDelay/backoffFactor) the uncapped multiply overflows TimeSpan.MaxValue and
            // TimeSpan.FromMilliseconds throws OverflowException *outside* the try - surfacing as an
            // unexpected fault instead of a clean retry/give-up. The sleep is already bounded by cappedDelay.
            var nextDelayMs = delay.TotalMilliseconds * _backoffFactor;
            delay = nextDelayMs >= TimeSpan.MaxValue.TotalMilliseconds
                ? TimeSpan.MaxValue
                : TimeSpan.FromMilliseconds(nextDelayMs);
        }
    }

    private static bool DefaultShouldRetry(Exception ex) => ex is not OperationCanceledException;

    private static bool DefaultShouldRetryContext(TContext context) => false;

    private static TimeSpan NoJitter(TimeSpan delay) => delay;
}
