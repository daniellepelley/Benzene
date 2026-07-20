using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core;

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
/// Creates one new DI scope per <see cref="HandleAsync"/> call and disposes it once the pipeline
/// finishes (and <paramref name="resultMapper"/> has extracted the result) - a scoped
/// <see cref="IDisposable"/> resolved during this event's pipeline is released before the next event
/// gets a fresh scope, not held open for the process's lifetime.
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
    public Task<TResult> HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
        => HandleAsync(@event, serviceResolverFactory, CancellationToken.None);

    /// <summary>
    /// Handles the event, additionally seeding the per-event scope's ambient cancellation token so any
    /// component resolved during the pipeline can observe cancellation via
    /// <see cref="ICancellationTokenAccessor"/>.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for dependency resolution.</param>
    /// <param name="cancellationToken">The transport's cancellation token for this event, or <see cref="CancellationToken.None"/> if it has no signal.</param>
    /// <returns>A task that represents the asynchronous operation, containing the processing result.</returns>
    public async Task<TResult> HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory, CancellationToken cancellationToken)
    {
        var context = mapper(@event);
        using var serviceResolver = serviceResolverFactory.CreateScope();
        serviceResolver.SeedCancellationToken(cancellationToken);
        await pipeline.HandleAsync(context, serviceResolver);
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
/// mapping events to contexts and executing the pipeline. Creates one new DI scope per
/// <see cref="HandleAsync"/> call and disposes it once the pipeline finishes - a scoped
/// <see cref="IDisposable"/> resolved during this event's pipeline is released before the next
/// event gets a fresh scope, not held open for the process's lifetime.
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
    public Task HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
        => HandleAsync(@event, serviceResolverFactory, CancellationToken.None);

    /// <summary>
    /// Handles the event, additionally seeding the per-event scope's ambient cancellation token so any
    /// component resolved during the pipeline can observe cancellation via
    /// <see cref="ICancellationTokenAccessor"/>.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for dependency resolution.</param>
    /// <param name="cancellationToken">The transport's cancellation token for this event, or <see cref="CancellationToken.None"/> if it has no signal.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory, CancellationToken cancellationToken)
    {
        var context = mapper(@event);
        using var serviceResolver = serviceResolverFactory.CreateScope();
        serviceResolver.SeedCancellationToken(cancellationToken);
        await pipeline.HandleAsync(context, serviceResolver);
    }
}
