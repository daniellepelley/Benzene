using System;
using Benzene.Abstractions.Middleware;
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
                using (var timer = processTimerFactory.Create(timerName))
                {
                    try
                    {
                        await next();
                    }
                    catch (Exception ex)
                    {
                        // The timer's Dispose can't observe the exception, so a span whose work threw
                        // would otherwise show as successful. Mark it failed here before it disposes -
                        // the OTel SDK reads these tags to set the span's status, matching what
                        // ActivityMiddlewareDecorator does with SetStatus(Error). Rethrow so behaviour
                        // is otherwise unchanged.
                        timer.SetTag("otel.status_code", "ERROR");
                        timer.SetTag("otel.status_description", ex.Message);
                        throw;
                    }
                }
            }
            else
            {
               await next();
            }
        }));
    }
    
    public static IMiddlewarePipelineBuilder<TContext> UseTimer<TContext>(this IMiddlewarePipelineBuilder<TContext> app, Action<TContext, long> onTimer)
    {
        return app.Use(new TimerMiddleware<TContext>(onTimer));
    }
}
