using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.Middleware;
using Benzene.Core.MiddlewareBuilder;

namespace Benzene.Diagnostics.Timers;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<TContext> UseTimer<TContext>(this IMiddlewarePipelineBuilder<TContext> source,
        string timerName)
    {
        return source.Use(resolver => new FuncWrapperMiddleware<TContext>(timerName, async (_, next) =>
        {
            var processTimerFactory = resolver.TryGetService<IProcessTimerFactory>();

            if (processTimerFactory != null)
            {
                using (processTimerFactory.Create(timerName))
                {
                    await next();
                }
            }
            else
            {
               await next();
            }
        }));
    }
}
