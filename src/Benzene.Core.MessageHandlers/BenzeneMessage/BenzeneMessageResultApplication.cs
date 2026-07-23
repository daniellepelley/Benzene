using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

/// <summary>
/// Runs the <c>BenzeneMessage</c> pipeline like <see cref="BenzeneMessageApplication"/> - same
/// <c>"benzene"</c> transport tag, same <see cref="BenzeneMessageContext"/> - but returns the
/// recorded <see cref="IBenzeneResult"/> (<see cref="BenzeneMessageContext.MessageResult"/>) instead
/// of the serialized <see cref="IBenzeneMessageResponse"/>.
/// </summary>
/// <remarks>
/// This is the entry point a one-way host uses when it needs the success/failure signal rather than a
/// response body - notably the envelope adapters (<c>BenzeneMessageEventHubHandler</c>,
/// <c>BenzeneMessageQueueStorageHandler</c>) that suppress the response but must surface the inner
/// handler's failure to the outer transport context so a <c>RaiseOnFailureStatus</c> escalation can
/// see it. <see cref="BenzeneMessageHandlerResultSetter"/> records the result onto the context even
/// when the response is suppressed, so the result is always populated for a routed message.
/// </remarks>
public class BenzeneMessageResultApplication : MiddlewareApplication<IBenzeneMessageRequest, BenzeneMessageContext, IBenzeneResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneMessageResultApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The middleware pipeline to run each request through.</param>
    public BenzeneMessageResultApplication(IMiddlewarePipeline<BenzeneMessageContext> pipeline)
        : base(
            new TransportMiddlewarePipeline<BenzeneMessageContext>(TransportNames.Benzene, pipeline),
            @event => new BenzeneMessageContext(@event),
            context => context.MessageResult)
    { }
}
