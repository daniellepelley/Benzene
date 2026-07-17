using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Serialization;
using Benzene.Core.Versioning.Schemas;

namespace Benzene.Core.Versioning.Response;

/// <summary>
/// Decorates the transport's real <see cref="IResponsePayloadMapper{TContext}"/>, transparently
/// downcasting the handler's canonical response payload into the version the request declared, so a
/// producer on an older version gets a response in its own version back
/// (docs/specification/versioning.md §4.2 - symmetric versioning by default). Delegates straight
/// through for failures (errors are a fixed error payload, not versioned), no version signal, no
/// resolvable topic/response type, a null or raw-string payload, or no registered downcast caster -
/// so a topic that doesn't opt into versioning is unaffected.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type responses are written for.</typeparam>
public class CastingResponsePayloadMapper<TContext> : IResponsePayloadMapper<TContext>
{
    private readonly IResponsePayloadMapper<TContext> _inner;
    private readonly IMessageVersionGetter<TContext> _versionGetter;
    private readonly IMessageTopicGetter<TContext> _topicGetter;
    private readonly ISchemaCasters? _schemaCasters;

    /// <summary>
    /// Initializes a new instance of the <see cref="CastingResponsePayloadMapper{TContext}"/> class.
    /// </summary>
    /// <param name="inner">The transport's real response payload mapper, used for the actual serialization (and every non-casting case).</param>
    /// <param name="versionGetter">Reads the requested payload schema version off the context (the same read the request path made - symmetric by default).</param>
    /// <param name="topicGetter">Reads the topic off the context, to scope the caster lookup.</param>
    /// <param name="schemaCasters">The registered schema casters, or <c>null</c> when none were registered (in which case this mapper always passes through).</param>
    public CastingResponsePayloadMapper(
        IResponsePayloadMapper<TContext> inner,
        IMessageVersionGetter<TContext> versionGetter,
        IMessageTopicGetter<TContext> topicGetter,
        ISchemaCasters? schemaCasters = null)
    {
        _inner = inner;
        _versionGetter = versionGetter;
        _topicGetter = topicGetter;
        _schemaCasters = schemaCasters;
    }

    /// <inheritdoc />
    public string Map(TContext context, IMessageHandlerResult messageHandlerResult, ISerializer serializer)
    {
        if (_schemaCasters == null || !messageHandlerResult.BenzeneResult.IsSuccessful)
        {
            return _inner.Map(context, messageHandlerResult, serializer);
        }

        var version = _versionGetter.GetVersion(context);
        if (string.IsNullOrEmpty(version))
        {
            return _inner.Map(context, messageHandlerResult, serializer);
        }

        var topic = _topicGetter.GetTopic(context)?.Id;
        var responseType = messageHandlerResult.MessageHandlerDefinition?.ResponseType;
        var payload = messageHandlerResult.BenzeneResult.PayloadAsObject;

        if (string.IsNullOrEmpty(topic) || responseType == null || payload == null || payload is IRawStringMessage)
        {
            return _inner.Map(context, messageHandlerResult, serializer);
        }

        if (!_schemaCasters.TryGetSchemaCaster(topic, responseType, version, out var caster))
        {
            return _inner.Map(context, messageHandlerResult, serializer);
        }

        var downcast = caster.Cast(payload);
        if (downcast == null)
        {
            return _inner.Map(context, messageHandlerResult, serializer);
        }

        var shim = new CastMessageHandlerResult(
            messageHandlerResult.Topic,
            new ResponseTypeOverrideDefinition(messageHandlerResult.MessageHandlerDefinition!, caster.ToType),
            messageHandlerResult.BenzeneResult,
            downcast);

        return _inner.Map(context, shim, serializer);
    }
}
