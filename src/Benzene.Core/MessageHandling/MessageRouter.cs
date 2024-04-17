using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Results;
using Benzene.Core.Results;
using Benzene.Results;

namespace Benzene.Core.MessageHandling;

public class MessageRouter<TContext> : IMiddleware<TContext> where TContext : IHasMessageResult
{
    private readonly IBenzeneLogger _logger;
    private readonly IMessageHandlerFactory _messageHandlerFactory;
    private readonly IMessageHandlersLookUp _messageHandlersLookUp;
    private readonly IMessageMapper<TContext> _messageMapper;
    private readonly IRequestMapper<TContext> _requestMapper;

    public MessageRouter(IMessageHandlerFactory messageHandlerFactory, IMessageMapper<TContext> messageMapper, IMessageHandlersLookUp messageHandlersLookUpUp, IRequestMapper<TContext> requestMapper, IBenzeneLogger logger)
    {
        _requestMapper = requestMapper;
        _messageHandlersLookUp = messageHandlersLookUpUp;
        _logger = logger;
        _messageMapper = messageMapper;
        _messageHandlerFactory = messageHandlerFactory;
    }

    public string Name => "MessageRouter";

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var topic = _messageMapper.GetTopic(context);
        if (string.IsNullOrEmpty(topic?.Id))
        {
            _logger.LogWarning("Topic is missing");
            context.MessageResult = ServiceResult.ValidationError("Topic is missing").AsMessageResult("<missing>", null);
            return;
        }

        _logger.LogDebug("Finding message handler for {topic}", topic.Id);
        Debug.WriteLine($"Finding message handler for {topic.Id}");
        var handlerDefinition = _messageHandlersLookUp.FindHandler(topic);
        if (handlerDefinition == null)
        {
            _logger.LogWarning("No handler found for topic {topic}", topic.Id);
            context.MessageResult = ServiceResult.NotFound($"No handler found for topic {topic.Id}").AsMessageResult(topic.Id, null);
            return;
        }

        var handler = _messageHandlerFactory.Create(handlerDefinition);
        if (handler == null)
        {
            _logger.LogWarning("No handler found for topic {topic}", topic.Id);
            context.MessageResult = ServiceResult.NotFound($"No handler found for topic {topic.Id}").AsMessageResult(topic.Id, null);
            return;
        }

        _logger.LogDebug("Handler mapped to topic");

        var result = await handler.HandlerAsync(new RequestFactory<TContext>(_requestMapper, context));
        context.MessageResult = result.AsMessageResult(topic.Id, handlerDefinition);
    }
}
