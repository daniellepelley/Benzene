using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.MessageHandlers.Info;

/// <summary>
/// Decorates an <see cref="IMiddlewarePipeline{TContext}"/>, recording the given transport name via
/// <see cref="ISetCurrentTransport"/> before delegating to the inner pipeline, so
/// <see cref="ICurrentTransport"/> reports the right transport for every message that flows through it.
/// </summary>
/// <typeparam name="TContext">The pipeline's context type.</typeparam>
/// <remarks>
/// Used, for example, to wrap the <c>BenzeneMessage</c> pipeline with the fixed transport name
/// <c>"benzene"</c> in <see cref="BenzeneMessage.BenzeneMessageApplication"/>.
/// </remarks>
public class TransportMiddlewarePipeline<TContext> : IMiddlewarePipeline<TContext>
{
    private readonly IMiddlewarePipeline<TContext> _pipeline;
    private readonly string _transport;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransportMiddlewarePipeline{TContext}"/> class.
    /// </summary>
    /// <param name="transport">The transport name to record for every invocation of this pipeline.</param>
    /// <param name="pipeline">The inner pipeline to delegate to.</param>
    public TransportMiddlewarePipeline(string transport, IMiddlewarePipeline<TContext> pipeline)
    {
        _transport = transport;
        _pipeline = pipeline;
    }

    /// <summary>
    /// Records the configured transport name as the current transport, then runs the inner pipeline.
    /// </summary>
    /// <param name="context">The context to process.</param>
    /// <param name="serviceResolver">Resolver used to resolve the registered <see cref="ISetCurrentTransport"/>, then passed through to the inner pipeline.</param>
    public Task HandleAsync(TContext context, IServiceResolver serviceResolver)
    {
        // Best-effort: the current-transport value is observability metadata (it feeds the
        // benzene.transport span tag / metrics dimension), so if ISetCurrentTransport isn't registered
        // - e.g. a minimal container that never called AddBenzene() - skip recording it rather than
        // failing the whole pipeline over a diagnostics concern.
        serviceResolver.TryGetService<ISetCurrentTransport>()?.SetTransport(_transport);
        return _pipeline.HandleAsync(context, serviceResolver);
    }
}
