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
            await _delay(_jitter(cappedDelay));
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * _backoffFactor);
        }
    }

    private static bool DefaultShouldRetry(Exception ex) => ex is not OperationCanceledException;

    private static bool DefaultShouldRetryContext(TContext context) => false;

    private static TimeSpan NoJitter(TimeSpan delay) => delay;
}
