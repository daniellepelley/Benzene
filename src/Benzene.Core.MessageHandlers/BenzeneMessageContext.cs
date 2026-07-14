using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IMessageHandlerContext{TRequest,TResponse}"/> implementation: the per-invocation
/// context flowing through a handler's middleware pipeline (see <see cref="HandlerPipelineBuilder"/>),
/// created fresh for each call by <see cref="PipelineMessageHandler{TRequest,TResponse}"/>.
/// </summary>
/// <typeparam name="TRequest">The strongly-typed request handled by this pipeline invocation.</typeparam>
/// <typeparam name="TResponse">The strongly-typed response payload produced by the handler.</typeparam>
/// <remarks>
/// Despite the file name, this class is <c>MessageHandlerContext&lt;TRequest, TResponse&gt;</c>, not
/// <c>BenzeneMessageContext</c> - that unrelated type (the transport context for the
/// <c>BenzeneMessage</c> format) lives in <c>Benzene.Core.Messages</c>. <see cref="Response"/> starts
/// out as an unexpected-error result so that a pipeline which never reaches the terminal handler
/// middleware (e.g. because earlier middleware throws) still ends with a non-null result.
/// </remarks>
public class MessageHandlerContext<TRequest, TResponse> : IMessageHandlerContext<TRequest, TResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHandlerContext{TRequest,TResponse}"/> class,
    /// defaulting <see cref="Response"/> to an unexpected-error result.
    /// </summary>
    /// <param name="topic">The topic the incoming message was routed on.</param>
    /// <param name="request">The strongly-typed request, already mapped from the transport payload.</param>
    /// <param name="handlerType">The concrete handler type resolved for this invocation, if known.</param>
    public MessageHandlerContext(ITopic topic, TRequest request, Type? handlerType = null)
    {
        Topic = topic;
        Request = request;
        HandlerType = handlerType;
        Response = BenzeneResult.UnexpectedError<TResponse>();
    }

    /// <inheritdoc />
    public ITopic Topic { get; }

    /// <inheritdoc />
    public Type? HandlerType { get; }

    /// <inheritdoc />
    public TRequest Request { get; }

    /// <inheritdoc />
    public IBenzeneResult<TResponse> Response { get; set; }
}
