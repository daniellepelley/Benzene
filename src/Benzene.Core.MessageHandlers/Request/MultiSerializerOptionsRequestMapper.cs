using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.MessageHandlers.Request;

/// <summary>
/// <see cref="IRequestMapper{TContext}"/> that asks the shared <see cref="IMediaFormatNegotiator{TContext}"/>
/// which format applies to the incoming context (falling back to JSON if none of the registered
/// <see cref="IMediaFormat{TContext}"/>s match), then maps the body (with enrichment) using that
/// format's serializer.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type requests are mapped from.</typeparam>
/// <remarks>
/// The composed <see cref="RequestMapper{TContext}"/>/<see cref="EnrichingRequestMapper{TContext}"/>
/// pair for a given resolved <see cref="ISerializer"/> is a pure function of (serializer, body getter,
/// enrichers) - none of which change for the lifetime of this (scoped, per-message) instance - so it's
/// built once per distinct serializer and cached, rather than reallocated on every <see cref="GetBody{TRequest}"/>
/// call within the same message.
/// </remarks>
public class MultiSerializerOptionsRequestMapper<TContext> : IRequestMapper<TContext>
{
    private readonly IMediaFormatNegotiator<TContext> _mediaFormatNegotiator;
    private readonly IServiceResolver _serviceResolver;
    private readonly IEnumerable<IRequestEnricher<TContext>> _enrichers;
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;
    private readonly IMessageBodyBytesGetter<TContext>? _messageBodyBytesGetter;
    private readonly Dictionary<ISerializer, IRequestMapper<TContext>> _mappersBySerializer = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiSerializerOptionsRequestMapper{TContext}"/> class.
    /// </summary>
    /// <param name="mediaFormatNegotiator">Selects which format to read the request body with.</param>
    /// <param name="serviceResolver">
    /// Resolver used to obtain the negotiated format's serializer, and to look up an optional
    /// <see cref="IMessageBodyBytesGetter{TContext}"/> for the byte-oriented mapping path.
    /// </param>
    /// <param name="messageBodyGetter">Extracts the raw message body from the context.</param>
    /// <param name="enrichers">The enrichers applied onto every mapped request.</param>
    public MultiSerializerOptionsRequestMapper(
        IMediaFormatNegotiator<TContext> mediaFormatNegotiator,
        IServiceResolver serviceResolver,
        IMessageBodyGetter<TContext> messageBodyGetter,
        IEnumerable<IRequestEnricher<TContext>> enrichers)
    {
        _mediaFormatNegotiator = mediaFormatNegotiator;
        _serviceResolver = serviceResolver;
        _messageBodyGetter = messageBodyGetter;
        _enrichers = enrichers;
        _messageBodyBytesGetter = serviceResolver.TryGetService<IMessageBodyBytesGetter<TContext>>();
    }

    /// <summary>
    /// Selects a serializer for <paramref name="context"/> and maps the body into
    /// <typeparamref name="TRequest"/>, applying request enrichment.
    /// </summary>
    /// <typeparam name="TRequest">The request type to map the body into.</typeparam>
    /// <param name="context">The transport-specific context for the incoming message.</param>
    /// <returns>The mapped and enriched request.</returns>
    public TRequest? GetBody<TRequest>(TContext context) where TRequest : class
    {
        var serializer = _mediaFormatNegotiator.SelectRead(context).GetSerializer(_serviceResolver);

        if (!_mappersBySerializer.TryGetValue(serializer, out var mapper))
        {
            mapper = new EnrichingRequestMapper<TContext>(
                new RequestMapper<TContext>(_messageBodyGetter, serializer, _messageBodyBytesGetter), _enrichers);
            _mappersBySerializer[serializer] = mapper;
        }

        return mapper.GetBody<TRequest>(context);
    }
}
