using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Messages;
using Benzene.Results;
using Microsoft.Extensions.Logging;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// The pipeline entry point for message-handler dispatch: extracts the topic from the incoming
/// context, looks up and creates the matching handler, invokes it, and hands the resulting
/// <see cref="IMessageHandlerResult"/> to the registered <see cref="IMessageHandlerResultSetter{TContext}"/>.
/// Registered as middleware via the <c>UseMessageHandlers</c> extension methods on <see cref="MiddlewarePipelineExtensions"/>.
/// </summary>
/// <typeparam name="TContext">The transport-specific pipeline context type.</typeparam>
/// <remarks>
/// If the topic is missing, no matching handler definition is found, or the factory can't create a
/// handler instance, the router short-circuits with an appropriate error result (validation error or
/// not-found, per <see cref="IDefaultStatuses"/>) instead of calling <c>next</c> in
/// <see cref="HandleAsync"/> — in all of these cases <c>next</c> is never invoked, so this middleware
/// is always the terminal step for message-handler dispatch.
/// </remarks>
public class MessageRouter<TContext> : IMiddleware<TContext>
{
    private readonly ILogger<MessageRouter<TContext>> _logger;
    private readonly IMessageHandlerFactory _messageHandlerFactory;
    private readonly IMessageHandlerDefinitionLookUp _messageHandlerDefinitionLookUp;
    private readonly IMessageGetter<TContext> _messageGetter;
    private readonly IMessageVersionGetter<TContext> _messageVersionGetter;
    private readonly IRequestMapper<TContext> _requestMapper;
    private readonly IDefaultStatuses _defaultStatuses;
    private readonly IMessageHandlerResultSetter<TContext> _messageHandlerResultSetter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageRouter{TContext}"/> class.
    /// </summary>
    /// <param name="messageHandlerFactory">Creates the invocable handler for a resolved definition.</param>
    /// <param name="messageGetter">Extracts the topic id (and other message data) from the context.</param>
    /// <param name="messageVersionGetter">Extracts the payload schema version from the context, combined with the topic id below into the <see cref="ITopic"/> used for handler-version dispatch (docs/specification/versioning.md §2.3).</param>
    /// <param name="messageHandlerDefinitionLookUpUp">Resolves the handler definition registered for a topic.</param>
    /// <param name="requestMapper">Maps the context into the handler's request type.</param>
    /// <param name="messageHandlerResultSetter">Writes the outcome of dispatch back onto the context.</param>
    /// <param name="defaultStatuses">Supplies the status codes used for routing failures (missing topic, no handler found).</param>
    /// <param name="logger">Logger used to record routing decisions and failures.</param>
    public MessageRouter(IMessageHandlerFactory messageHandlerFactory,
        IMessageGetter<TContext> messageGetter,
        IMessageVersionGetter<TContext> messageVersionGetter,
        IMessageHandlerDefinitionLookUp messageHandlerDefinitionLookUpUp,
        IRequestMapper<TContext> requestMapper,
        IMessageHandlerResultSetter<TContext> messageHandlerResultSetter,
        IDefaultStatuses defaultStatuses,
        ILogger<MessageRouter<TContext>> logger)
    {
        _messageHandlerResultSetter = messageHandlerResultSetter;
        _defaultStatuses = defaultStatuses;
        _requestMapper = requestMapper;
        _messageHandlerDefinitionLookUp = messageHandlerDefinitionLookUpUp;
        _logger = logger;
        _messageGetter = messageGetter;
        _messageVersionGetter = messageVersionGetter;
        _messageHandlerFactory = messageHandlerFactory;
    }

    /// <inheritdoc />
    public string Name => "MessageRouter";

    /// <summary>
    /// Extracts the topic from <paramref name="context"/>, resolves and invokes the matching handler,
    /// and writes the result back via the registered <see cref="IMessageHandlerResultSetter{TContext}"/>.
    /// </summary>
    /// <param name="context">The current pipeline context.</param>
    /// <param name="next">Unused - this middleware never calls the rest of the pipeline (see remarks).</param>
    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var topic = _messageGetter.GetTopic(context);
        // A version already on the topic (e.g. an explicit UsePresetTopic(topicId, version)) is a
        // deliberate override and wins; the message's own version signal only fills the gap when
        // the topic getter didn't already supply one.
        if (!string.IsNullOrEmpty(topic?.Id) && string.IsNullOrEmpty(topic.Version))
        {
            var version = _messageVersionGetter.GetVersion(context);
            if (!string.IsNullOrEmpty(version))
            {
                topic = new Topic(topic.Id, version);
            }
        }

        if (string.IsNullOrEmpty(topic?.Id))
        {
            _logger.LogWarning("Topic is missing");
            await _messageHandlerResultSetter.SetResultAsync(context, new MessageHandlerResult(topic, MessageHandlerDefinition.Empty(), BenzeneResult.Set( _defaultStatuses.ValidationError, "Topic is missing")));
            return;
        }

        _logger.LogDebug("Finding message handler for {topic}", topic.Id);
        var messageHandlerDefinition = _messageHandlerDefinitionLookUp.FindHandler(topic);
        if (messageHandlerDefinition == null)
        {
            _logger.LogWarning("No handler found for topic {topic}", topic.Id);
            await _messageHandlerResultSetter.SetResultAsync(context, new MessageHandlerResult(topic, MessageHandlerDefinition.Empty(), BenzeneResult.Set(_defaultStatuses.NotFound, $"No handler found for topic {topic.Id}")));
            return;
        }

        var handler = _messageHandlerFactory.Create(messageHandlerDefinition);
        if (handler == null)
        {
            _logger.LogWarning("No handler found for topic {topic}", topic.Id);
            await _messageHandlerResultSetter.SetResultAsync(context, new MessageHandlerResult(topic, messageHandlerDefinition, BenzeneResult.Set(_defaultStatuses.NotFound, $"No handler found for topic {topic.Id}")));
            return;
        }

        _logger.LogDebug("Handler mapped to topic");

        var result = await handler.HandleAsync(new DeferredRequestMapper<TContext>(_requestMapper, context));

        // A baseline failure signal even when no logging middleware is wired: an unsuccessful handler
        // result (BadRequest/NotFound/UnexpectedError/...) is otherwise invisible in logs - the router
        // only logged routing failures, and UseLogResult logs every result at Information. Warn once,
        // with the topic, status, and any error messages, so "show me the errors" surfaces it.
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Handler {handler} for topic {topic} returned unsuccessful status {status}{errors}",
                messageHandlerDefinition.HandlerType.Name, topic.Id, result.Status,
                result.Errors.Length > 0 ? " - " + string.Join("; ", result.Errors) : string.Empty);
        }

        await _messageHandlerResultSetter.SetResultAsync(context, new MessageHandlerResult(topic, messageHandlerDefinition, result));
    }
}
