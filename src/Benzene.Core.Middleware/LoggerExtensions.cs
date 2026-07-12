using System.Diagnostics;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Logging;
using Microsoft.Extensions.Logging;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides extension methods for adding logging middleware to pipelines.
/// </summary>
public static class LoggerExtensions
{
    private const string LoggerCategory = "Benzene";

    /// <summary>
    /// Adds middleware that logs the request, response, and processing time for each pipeline execution.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add logging to.</param>
    /// <param name="action">The action that configures the log context builder.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This middleware measures the processing time and emits a single structured log line per execution.
    /// The properties configured via the log context builder are attached as logger scopes, so they also
    /// enrich every other log statement made within the pipeline execution.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> UseLogResult<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, Action<ILogContextBuilder<TContext>> action)
    {
        ILogContextBuilder<TContext> builder = new LogContextBuilder<TContext>(app);
        action(builder);

        return app
            .Use("LogResult", resolver => async (context, next) =>
            {
                var logger = resolver.GetService<ILoggerFactory>().CreateLogger(LoggerCategory);
                var stopwatch = Stopwatch.StartNew();
                using (logger.BeginScope(builder.BuildRequestScope(resolver, context)))
                {
                    await next();

                    using (logger.BeginScope(builder.BuildResponseScope(resolver, context)))
                    using (logger.BeginScope(new Dictionary<string, object> { ["processTime"] = stopwatch.ElapsedMilliseconds }))
                    {
                        logger.LogInformation("BenzeneResult");
                    }
                }
            });
    }

    /// <summary>
    /// Adds middleware that enriches the logging scope for the duration of the request.
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
        ILogContextBuilder<TContext> builder = new LogContextBuilder<TContext>(app);
        action(builder);

        return app
            .Use("LogContext", resolver => async (context, next) =>
            {
                var logger = resolver.GetService<ILoggerFactory>().CreateLogger(LoggerCategory);
                using (logger.BeginScope(builder.BuildRequestScope(resolver, context)))
                {
                    await next();
                }
            });
    }
}
