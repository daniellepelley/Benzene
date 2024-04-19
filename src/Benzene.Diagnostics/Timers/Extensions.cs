using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.Middleware;

namespace Benzene.Diagnostics.Timers;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<TContext> UseTimer<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string timerName)
    {
        return app.Use(resolver => new FuncWrapperMiddleware<TContext>(timerName, async (_, next) =>
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
