using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Core.MessageHandlers.Response;

/// <summary>
/// <see cref="IMessageHandlerResultSetter{TContext}"/> that only writes a response if routing actually
/// reached a real topic (i.e. <see cref="IMessageHandlerResultBase.Topic"/> is set and isn't the
/// <see cref="Constants.Missing"/> sentinel), so transports that pass through unrelated/unroutable
/// traffic don't get a response written for it.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public class ResponseIfHandledMessageHandlerResultSetter<TContext> : IMessageHandlerResultSetter<TContext> where TContext : class
{
    private readonly IResponseHandlerContainer<TContext> _responseHandlerContainer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseIfHandledMessageHandlerResultSetter{TContext}"/> class.
    /// </summary>
    /// <param name="responseHandlerContainer">Runs the registered response handlers when a topic was actually routed.</param>
    public ResponseIfHandledMessageHandlerResultSetter(IResponseHandlerContainer<TContext> responseHandlerContainer)
    {
        _responseHandlerContainer = responseHandlerContainer;
    }

    /// <summary>
    /// Writes the response via the response handler container, but only if
    /// <paramref name="messageHandlerResult"/> carries a real (non-missing) topic.
    /// </summary>
    /// <param name="context">The transport context to write the response onto.</param>
    /// <param name="messageHandlerResult">The outcome of routing and handling the message.</param>
    public async Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        if (messageHandlerResult.Topic != null && messageHandlerResult.Topic.Id != Constants.Missing.Id)
        {
            await _responseHandlerContainer.HandleAsync(context, messageHandlerResult);
        }
    }
}

