using System;
using System.Diagnostics;
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
                // Capture the ambient span so we can tell which one (if any) the timer opens: an
                // ActivityProcessTimer sets Activity.Current to its own span for the duration of the
                // using, whereas non-Activity timer backends (Logging/Debug) leave it unchanged.
                var outerActivity = Activity.Current;
                using (processTimerFactory.Create(timerName))
                {
                    try
                    {
                        await next();
                    }
                    catch (Exception ex)
                    {
                        // The timer's Dispose() ends its span but can't observe whether the wrapped
                        // work threw, so a failed span would otherwise show as successful in a trace
                        // viewer. Mark only the span the timer actually opened (skips non-Activity
                        // backends and the unsampled case), then rethrow untouched - observe, don't handle.
                        var timerActivity = Activity.Current;
                        if (timerActivity != null && !ReferenceEquals(timerActivity, outerActivity))
                        {
                            timerActivity.AddException(ex);
                            timerActivity.SetStatus(ActivityStatusCode.Error, ex.Message);
                        }

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
