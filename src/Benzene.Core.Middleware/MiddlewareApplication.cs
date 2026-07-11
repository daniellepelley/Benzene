using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides a middleware application that processes events through a pipeline and returns results.
/// </summary>
/// <typeparam name="TEvent">The type of event to process.</typeparam>
/// <typeparam name="TContext">The type of context created from the event.</typeparam>
/// <typeparam name="TResult">The type of result returned after processing.</typeparam>
/// <remarks>
/// This application serves as an adapter between external events and the middleware pipeline,
/// mapping events to contexts, executing the pipeline, and extracting results from the processed context.
/// </remarks>
public class MiddlewareApplication<TEvent, TContext, TResult>(
    IMiddlewarePipeline<TContext> pipeline,
    Func<TEvent, TContext> mapper,
    Func<TContext, TResult> resultMapper)
    : IMiddlewareApplication<TEvent, TResult>
{
    /// <summary>
    /// Handles the event by mapping it to a context, executing the pipeline, and returning the result.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for dependency resolution.</param>
    /// <returns>A task that represents the asynchronous operation, containing the processing result.</returns>
    public async Task<TResult> HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var context = mapper(@event);
        await pipeline.HandleAsync(context, serviceResolverFactory.CreateScope());
        return resultMapper(context);
    }
}

/// <summary>
/// Provides a middleware application that processes events through a pipeline without returning results.
/// </summary>
/// <typeparam name="TEvent">The type of event to process.</typeparam>
/// <typeparam name="TContext">The type of context created from the event.</typeparam>
/// <remarks>
/// This application serves as an adapter between external events and the middleware pipeline,
/// mapping events to contexts and executing the pipeline.
/// </remarks>
public class MiddlewareApplication<TEvent, TContext>(
    IMiddlewarePipeline<TContext> pipeline,
    Func<TEvent, TContext> mapper)
    : IMiddlewareApplication<TEvent>
{
    /// <summary>
    /// Handles the event by mapping it to a context and executing the pipeline.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for dependency resolution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var context = mapper(@event);
        await pipeline.HandleAsync(context, serviceResolverFactory.CreateScope());
    }
}
