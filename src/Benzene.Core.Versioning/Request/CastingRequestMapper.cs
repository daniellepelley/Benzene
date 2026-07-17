using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Core.Versioning.Schemas;

namespace Benzene.Core.Versioning.Request;

/// <summary>
/// Decorates the transport's real <see cref="IRequestMapper{TContext}"/>, transparently upcasting an
/// older-version request payload into the handler's declared request type
/// (docs/specification/versioning.md §4.1). When no version is signalled, no topic resolves, or no
/// caster is registered for <c>(topic, version -> TRequest)</c>, it delegates straight through - so a
/// topic that doesn't opt into versioning behaves exactly as it did before, with zero overhead.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type requests are mapped from.</typeparam>
public class CastingRequestMapper<TContext> : IRequestMapper<TContext>
{
    private readonly IRequestMapper<TContext> _inner;
    private readonly IMessageVersionGetter<TContext> _versionGetter;
    private readonly IMessageTopicGetter<TContext> _topicGetter;
    private readonly ISchemaCasters? _schemaCasters;

    /// <summary>
    /// Initializes a new instance of the <see cref="CastingRequestMapper{TContext}"/> class.
    /// </summary>
    /// <param name="inner">The transport's real request mapper, used for the actual deserialization (and for every non-casting case).</param>
    /// <param name="versionGetter">Reads the incoming payload schema version off the context.</param>
    /// <param name="topicGetter">Reads the topic off the context, to scope the caster lookup.</param>
    /// <param name="schemaCasters">The registered schema casters, or <c>null</c> when none were registered (in which case this mapper always passes through).</param>
    public CastingRequestMapper(
        IRequestMapper<TContext> inner,
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
    public TRequest? GetBody<TRequest>(TContext context) where TRequest : class
    {
        if (_schemaCasters == null)
        {
            return _inner.GetBody<TRequest>(context);
        }

        var version = _versionGetter.GetVersion(context);
        if (string.IsNullOrEmpty(version))
        {
            return _inner.GetBody<TRequest>(context);
        }

        var topic = _topicGetter.GetTopic(context)?.Id;
        if (string.IsNullOrEmpty(topic))
        {
            return _inner.GetBody<TRequest>(context);
        }

        if (!_schemaCasters.TryGetSchemaCaster(topic, version, typeof(TRequest), out var caster))
        {
            return _inner.GetBody<TRequest>(context);
        }

        // Deserialize the wire body as the incoming version's CLR shape (still via the inner mapper, so
        // the negotiated serializer, byte-oriented fast path, and empty-body defaulting all apply), then
        // upcast into TRequest.
        var incoming = RequestBodyReader<TContext>.Read(_inner, context, caster.FromType);
        if (incoming == null)
        {
            return null;
        }

        return (TRequest?)caster.Cast(incoming);
    }
}
