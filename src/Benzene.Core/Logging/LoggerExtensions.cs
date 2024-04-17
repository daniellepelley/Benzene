using System;
using System.Diagnostics;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.MiddlewareBuilder;

namespace Benzene.Core.Logging;

public static class LoggerExtensions
{
    public static IMiddlewarePipelineBuilder<TContext> UseLogResult<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, Action<LogContextBuilder<TContext>> action)
    {
        var builder = new LogContextBuilder<TContext>(app);
        action(builder);

        return app
            .Use("LogResult", resolver => async (context, next) =>
            {
                var logContext = resolver.GetService<IBenzeneLogContext>();
                var logger = resolver.GetService<IBenzeneLogger>();
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                using (builder.CreateForRequest(logContext, resolver, context))
                {
                    await next();

                    using (builder.CreateForResponse(logContext, resolver, context))
                    {
                        using (logContext.Create("processTime", $"{stopwatch.ElapsedMilliseconds}"))
                        {
                            logger.LogInformation("Result");
                        }
                    }
                }
            });
    }
    
    public static IMiddlewarePipelineBuilder<TContext> UseLogContext<TContext>(
            this IMiddlewarePipelineBuilder<TContext> app, Action<LogContextBuilder<TContext>> action)
    {
        var builder = new LogContextBuilder<TContext>(app);
        action(builder);
    
        return app
            .Use("LogContext", resolver => async (context, next) =>
            {
                var logContext = resolver.GetService<IBenzeneLogContext>();
                using (builder.CreateForRequest(logContext, resolver, context))
                {
                    await next();
                }
            });
    }
}
