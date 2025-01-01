using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.MessageHandlers;

public class MessageHandlerMiddleware<TRequest, TResponse> : IMiddleware<IMessageHandlerContext<TRequest, TResponse>>
{
    private readonly IMessageHandler<TRequest, TResponse> _messageHandler;

    public MessageHandlerMiddleware(IMessageHandler<TRequest, TResponse> messageHandler)
    {
        _messageHandler = messageHandler;
    }

    public string Name => "MessageHandler";

    public async Task HandleAsync(IMessageHandlerContext<TRequest, TResponse> context, Func<Task> next)
    {
        context.Response = await _messageHandler.HandleAsync(context.Request);
    }
}