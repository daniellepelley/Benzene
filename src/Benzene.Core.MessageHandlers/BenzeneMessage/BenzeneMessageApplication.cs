using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

/// <summary>
/// The application entry point for the <c>BenzeneMessage</c> transport-agnostic message format:
/// wraps the request pipeline into a <see cref="TransportMiddlewarePipeline{TContext}"/> tagged with
/// the <c>"benzene"</c> transport name, converts an incoming <see cref="IBenzeneMessageRequest"/> into
/// a <see cref="BenzeneMessageContext"/>, and returns the resulting <see cref="IBenzeneMessageResponse"/>.
/// </summary>
/// <remarks>
/// Use this to invoke a Benzene pipeline directly with an in-process/programmatic message rather than
/// through a specific network transport, e.g. from tests or from another adapter that already has a
/// <see cref="IBenzeneMessageRequest"/> to hand off.
/// </remarks>
public class BenzeneMessageApplication : MiddlewareApplication<IBenzeneMessageRequest, BenzeneMessageContext, IBenzeneMessageResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneMessageApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The middleware pipeline to run each request through.</param>
    public BenzeneMessageApplication(IMiddlewarePipeline<BenzeneMessageContext> pipeline)
        : base(
            new TransportMiddlewarePipeline<BenzeneMessageContext>("benzene", pipeline),
            @event => new BenzeneMessageContext(@event),
            context => context.BenzeneMessageResponse)
    { }
}
