using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.SelfHost.Http;

/// <summary>
/// Sets the message handler result onto a <see cref="SelfHostHttpContext"/>'s response, running the
/// registered <see cref="IResponseHandler{TContext}"/> chain (status code, body, etc.) via the shared
/// <see cref="ResponseMessageHandlerResultSetterBase{TContext}"/> behavior - the same pattern
/// used by every other HTTP-shaped transport (<c>AspMessageHandlerResultSetter</c>,
/// <c>ApiGatewayMessageHandlerResultSetter</c>).
/// </summary>
public class HttpListenerMessageHandlerResultSetter : ResponseMessageHandlerResultSetterBase<SelfHostHttpContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpListenerMessageHandlerResultSetter"/> class.
    /// </summary>
    /// <param name="responseHandlerContainer">Runs the registered response handlers to produce the outbound response.</param>
    public HttpListenerMessageHandlerResultSetter(IResponseHandlerContainer<SelfHostHttpContext> responseHandlerContainer)
        : base(responseHandlerContainer)
    {
    }
}
