using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides a middleware application that processes multiple contexts from a single event in parallel,
/// returning an array of results.
/// </summary>
/// <typeparam name="TEvent">The type of event to process.</typeparam>
/// <typeparam name="TContext">The type of context created from the event.</typeparam>
/// <typeparam name="TResult">The type of result returned for each context.</typeparam>
/// <remarks>
/// This application enables batch processing scenarios where a single event produces multiple contexts
/// that can be processed concurrently through the same pipeline. Each context is processed in its own
/// service scope, ensuring isolation between concurrent executions.
/// </remarks>
public class MiddlewareMultiApplication<TEvent, TContext, TResult>(
    IMiddlewarePipeline<TContext> pipelineBuilder,
    Func<TEvent, TContext[]> mapper,
    Func<TContext, TResult> resultMapper)
    : IMiddlewareApplication<TEvent, TResult[]>
{
    /// <summary>
    /// Handles the event by mapping it to multiple contexts, processing them in parallel, and returning all results.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for dependency resolution.</param>
    /// <returns>A task that represents the asynchronous operation, containing an array of processing results.</returns>
    public Task<TResult[]> HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var tasks = mapper(@event).Select(async context =>
            {
                using var scope = serviceResolverFactory.CreateScope();
                await pipelineBuilder.HandleAsync(context, scope);
                return resultMapper(context);
            })
            .ToArray();

        return Task.WhenAll(tasks);
    }
}

/// <summary>
/// Provides a middleware application that processes multiple contexts from a single event in parallel,
/// without returning results.
/// </summary>
/// <typeparam name="TEvent">The type of event to process.</typeparam>
/// <typeparam name="TContext">The type of context created from the event.</typeparam>
/// <remarks>
/// This application enables batch processing scenarios where a single event produces multiple contexts
/// that can be processed concurrently through the same pipeline. Each context is processed in its own
/// service scope, ensuring isolation between concurrent executions.
/// </remarks>
public class MiddlewareMultiApplication<TEvent, TContext>(
    IMiddlewarePipeline<TContext> pipeline,
    Func<TEvent, TContext[]> mapper)
    : IMiddlewareApplication<TEvent>
{
    /// <summary>
    /// Handles the event by mapping it to multiple contexts and processing them in parallel.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for dependency resolution.</param>
    /// <returns>A task representing the asynchronous operation that completes when all contexts are processed.</returns>
    public Task HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var tasks = mapper(@event).Select(async context =>
            {
                using var scope = serviceResolverFactory.CreateScope();
                await pipeline.HandleAsync(context, scope);
            })
            .ToArray();

        return Task.WhenAll(tasks);
    }
}


