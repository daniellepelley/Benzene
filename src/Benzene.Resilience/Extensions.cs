using Benzene.Abstractions.Middleware;

namespace Benzene.Resilience;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<TContext> UseRetry<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        int numberOfRetries = 3,
        TimeSpan? initialDelay = null,
        double backoffFactor = 2.0,
        Func<Exception, bool>? shouldRetry = null,
        Func<TContext, bool>? shouldRetryContext = null,
        Func<TimeSpan, Task>? delay = null)
    {
        return app.Use(_ => new RetryMiddleware<TContext>(
            numberOfRetries, initialDelay, backoffFactor, shouldRetry, shouldRetryContext, delay));
    }
}
