using System.Diagnostics;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Middleware;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

public class MessageRouter<TContext> : IMiddleware<TContext>
{
    private readonly IBenzeneLogger _logger;
    private readonly IMessageHandlerFactory _messageHandlerFactory;
    private readonly IMessageHandlerDefinitionLookUp _messageHandlerDefinitionLookUp;
    private readonly IMessageGetter<TContext> _messageGetter;
    private readonly IRequestMapper<TContext> _requestMapper;
    private readonly IDefaultStatuses _defaultStatuses;
    private readonly IMessageHandlerResultSetter<TContext> _messageHandlerResultSetter;

    public MessageRouter(IMessageHandlerFactory messageHandlerFactory,
        IMessageGetter<TContext> messageGetter,
        IMessageHandlerDefinitionLookUp messageHandlerDefinitionLookUpUp,
        IRequestMapper<TContext> requestMapper,
        IMessageHandlerResultSetter<TContext> messageHandlerResultSetter,
        IDefaultStatuses defaultStatuses,
        IBenzeneLogger logger)
    {
        _messageHandlerResultSetter = messageHandlerResultSetter;
        _defaultStatuses = defaultStatuses;
        _requestMapper = requestMapper;
        _messageHandlerDefinitionLookUp = messageHandlerDefinitionLookUpUp;
        _logger = logger;
        _messageGetter = messageGetter;
        _messageHandlerFactory = messageHandlerFactory;
    }

    public string Name => "MessageRouter";

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var topic = _messageGetter.GetTopic(context);
        if (string.IsNullOrEmpty(topic?.Id))
        {
            _logger.LogWarning("Topic is missing");
            _messageHandlerResultSetter.SetResultAsync(context, new MessageHandlerResult(topic, MessageHandlerDefinition.Empty(), BenzeneResult.Set( _defaultStatuses.ValidationError, "Topic is missing")));
            return;
        }

        _logger.LogDebug("Finding message handler for {topic}", topic.Id);
        Debug.WriteLine($"Finding message handler for {topic.Id}");
        var messageHandlerDefinition = _messageHandlerDefinitionLookUp.FindHandler(topic);
        if (messageHandlerDefinition == null)
        {
            _logger.LogWarning("No handler found for topic {topic}", topic.Id);
            _messageHandlerResultSetter.SetResultAsync(context, new MessageHandlerResult(topic, MessageHandlerDefinition.Empty(), BenzeneResult.Set(_defaultStatuses.NotFound, $"No handler found for topic {topic.Id}")));
            return;
        }

        var handler = _messageHandlerFactory.Create(messageHandlerDefinition);
        if (handler == null)
        {
            _logger.LogWarning("No handler found for topic {topic}", topic.Id);
            _messageHandlerResultSetter.SetResultAsync(context, new MessageHandlerResult(topic, messageHandlerDefinition, BenzeneResult.Set(_defaultStatuses.NotFound, $"No handler found for topic {topic.Id}"))); 
            return;
        }

        _logger.LogDebug("Handler mapped to topic");

        var result = await handler.HandlerAsync(new RequestMapperThunk<TContext>(_requestMapper, context));
        _messageHandlerResultSetter.SetResultAsync(context, new MessageHandlerResult(topic, messageHandlerDefinition, result));
    }
}
