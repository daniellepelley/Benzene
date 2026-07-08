using Benzene.Abstractions.Middleware;

namespace Benzene.Resilience;

public class RetryMiddleware<TContext> : IMiddleware<TContext>
{
    private readonly int _numberOfRetries;
    private readonly TimeSpan _initialDelay;
    private readonly double _backoffFactor;
    private readonly Func<Exception, bool> _shouldRetry;
    private readonly Func<TContext, bool> _shouldRetryContext;
    private readonly Func<TimeSpan, Task> _delay;

    public RetryMiddleware(
        int numberOfRetries = 3,
        TimeSpan? initialDelay = null,
        double backoffFactor = 2.0,
        Func<Exception, bool>? shouldRetry = null,
        Func<TContext, bool>? shouldRetryContext = null,
        Func<TimeSpan, Task>? delay = null)
    {
        _numberOfRetries = numberOfRetries;
        _initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(200);
        _backoffFactor = backoffFactor;
        _shouldRetry = shouldRetry ?? DefaultShouldRetry;
        _shouldRetryContext = shouldRetryContext ?? DefaultShouldRetryContext;
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
            await _delay(delay);
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * _backoffFactor);
        }
    }

    private static bool DefaultShouldRetry(Exception ex) => ex is not OperationCanceledException;

    private static bool DefaultShouldRetryContext(TContext context) => false;
}
