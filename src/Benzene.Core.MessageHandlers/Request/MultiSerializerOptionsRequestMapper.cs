using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.MessageHandlers.Request;

/// <summary>
/// <see cref="IRequestMapper{TContext}"/> that selects between several registered
/// <see cref="ISerializerOption{TContext}"/>s based on the incoming context, falling back to
/// <typeparamref name="TDefaultSerializer"/> if none apply, then maps the body (with enrichment)
/// using whichever serializer was selected.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type requests are mapped from.</typeparam>
/// <typeparam name="TDefaultSerializer">The serializer used when no registered <see cref="ISerializerOption{TContext}"/> matches the context.</typeparam>
public class MultiSerializerOptionsRequestMapper<TContext, TDefaultSerializer> : IRequestMapper<TContext>
    where TDefaultSerializer : class, ISerializer
{
    private readonly IEnumerable<ISerializerOption<TContext>> _options;
    private readonly IServiceResolver _serviceResolver;
    private readonly IEnumerable<IRequestEnricher<TContext>> _enrichers;
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiSerializerOptionsRequestMapper{TContext,TDefaultSerializer}"/> class.
    /// </summary>
    /// <param name="serviceResolver">Resolver used to obtain the selected serializer and, if none match, <typeparamref name="TDefaultSerializer"/>.</param>
    /// <param name="messageBodyGetter">Extracts the raw message body from the context.</param>
    /// <param name="options">The candidate serializer options to evaluate against each context.</param>
    /// <param name="enrichers">The enrichers applied onto every mapped request.</param>
    public MultiSerializerOptionsRequestMapper(
        IServiceResolver serviceResolver,
        IMessageBodyGetter<TContext> messageBodyGetter,
        IEnumerable<ISerializerOption<TContext>> options,
        IEnumerable<IRequestEnricher<TContext>> enrichers)
    {
        _messageBodyGetter = messageBodyGetter;
        _enrichers = enrichers;
        _options = options;
        _serviceResolver = serviceResolver;
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
        var mapper = GetMapper(context);
        return mapper.GetBody<TRequest>(context);
    }

    private IRequestMapper<TContext> GetMapper(TContext context)
    {
        var serializerOption = _options.FirstOrDefault(option => option.CanHandle.Check(context, _serviceResolver));
        var serializer = serializerOption != null
            ? serializerOption.GetSerializer(_serviceResolver)
            : _serviceResolver.GetService<TDefaultSerializer>();

        return new EnrichingRequestMapper<TContext>(
                        new RequestMapper<TContext>(_messageBodyGetter, serializer),
                        _enrichers);
    }
}
