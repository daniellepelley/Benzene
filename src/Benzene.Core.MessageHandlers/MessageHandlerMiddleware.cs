using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// The terminal middleware in a handler's pipeline (see <see cref="HandlerPipelineBuilder"/>):
/// invokes the strongly-typed handler and assigns its result onto <see cref="IMessageHandlerContext{TRequest,TResponse}.Response"/>.
/// </summary>
/// <typeparam name="TRequest">The strongly-typed request handled by this pipeline.</typeparam>
/// <typeparam name="TResponse">The strongly-typed response produced by the handler.</typeparam>
/// <remarks>
/// Always appended as the last step by <see cref="HandlerPipelineBuilder"/>, so it does not call
/// <c>next</c> - there is nothing further in the pipeline after the handler itself runs.
/// </remarks>
public class MessageHandlerMiddleware<TRequest, TResponse> : IMiddleware<IMessageHandlerContext<TRequest, TResponse>>
{
    private readonly IMessageHandler<TRequest, TResponse> _messageHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHandlerMiddleware{TRequest,TResponse}"/> class.
    /// </summary>
    /// <param name="messageHandler">The handler to invoke.</param>
    public MessageHandlerMiddleware(IMessageHandler<TRequest, TResponse> messageHandler)
    {
        _messageHandler = messageHandler;
    }

    /// <inheritdoc />
    public string Name => "MessageHandler";

    /// <summary>
    /// Invokes the wrapped handler with <see cref="IMessageHandlerContext{TRequest,TResponse}.Request"/>
    /// and stores its result on <see cref="IMessageHandlerContext{TRequest,TResponse}.Response"/>.
    /// </summary>
    /// <param name="context">The current handler invocation's context.</param>
    /// <param name="next">Unused - this is always the last middleware in the pipeline.</param>
    public async Task HandleAsync(IMessageHandlerContext<TRequest, TResponse> context, Func<Task> next)
    {
        context.Response = await _messageHandler.HandleAsync(context.Request);
    }
}
