﻿using System.Diagnostics;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

public class MessageRouter<TContext> : IMiddleware<TContext> where TContext : IHasMessageResult
{
    private readonly IBenzeneLogger _logger;
    private readonly IMessageHandlerFactory _messageHandlerFactory;
    private readonly IMessageHandlersLookUp _messageHandlersLookUp;
    private readonly IMessageMapper<TContext> _messageMapper;
    private readonly IRequestMapper<TContext> _requestMapper;
    private IDefaultStatuses _defaultStatuses;

    public MessageRouter(IMessageHandlerFactory messageHandlerFactory, IMessageMapper<TContext> messageMapper, IMessageHandlersLookUp messageHandlersLookUpUp, IRequestMapper<TContext> requestMapper, IDefaultStatuses defaultStatuses, IBenzeneLogger logger)
    {
        _defaultStatuses = defaultStatuses;
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
            context.MessageResult = MessageResult.Failure(_defaultStatuses.ValidationError, "Topic is missing");
            return;
        }

        _logger.LogDebug("Finding message handler for {topic}", topic.Id);
        Debug.WriteLine($"Finding message handler for {topic.Id}");
        var handlerDefinition = _messageHandlersLookUp.FindHandler(topic);
        if (handlerDefinition == null)
        {
            _logger.LogWarning("No handler found for topic {topic}", topic.Id);
            context.MessageResult = MessageResult.Failure(topic, _defaultStatuses.NotFound, $"No handler found for topic {topic.Id}");
            return;
        }

        var handler = _messageHandlerFactory.Create(handlerDefinition);
        if (handler == null)
        {
            _logger.LogWarning("No handler found for topic {topic}", topic.Id);
            context.MessageResult = ServiceResult.NotFound($"No handler found for topic {topic.Id}").AsMessageResult(topic, null);
            return;
        }

        _logger.LogDebug("Handler mapped to topic");

        var result = await handler.HandlerAsync(new RequestFactory<TContext>(_requestMapper, context));
        context.MessageResult = result.AsMessageResult(topic, handlerDefinition);
    }
}
