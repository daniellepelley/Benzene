using System.Diagnostics;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Logging;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides extension methods for adding logging middleware to pipelines.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Adds middleware that logs the request, response, and processing time for each pipeline execution.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add logging to.</param>
    /// <param name="action">The action that configures the log context builder.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This middleware measures the processing time and logs structured information about the request
    /// and response, including any properties configured via the log context builder.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> UseLogResult<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, Action<ILogContextBuilder<TContext>> action)
    {
        ILogContextBuilder<TContext> builder = new LogContextBuilder<TContext>(app);
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
                            logger.LogInformation("BenzeneResult");
                        }
                    }
                }
            });
    }

    /// <summary>
    /// Adds middleware that enriches the logging context for the duration of the request.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add logging context to.</param>
    /// <param name="action">The action that configures the log context builder.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This middleware adds context-specific properties to the logging scope, making them available
    /// to all log statements within the pipeline execution.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> UseLogContext<TContext>(
            this IMiddlewarePipelineBuilder<TContext> app, Action<ILogContextBuilder<TContext>> action)
    {
        ILogContextBuilder<TContext>  builder = new LogContextBuilder<TContext>(app);
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
