using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Core.MessageHandlers.Request;

/// <summary>
/// The default <see cref="IRequestMapper{TContext}"/> registered by <c>AddContextItems</c>: a
/// <see cref="MultiSerializerOptionsRequestMapper{TContext,TDefaultSerializer}"/> whose fallback
/// serializer is the package's <see cref="JsonSerializer"/>, so JSON works out of the box while still
/// allowing other serializers to be selected via registered <c>ISerializerOption{TContext}</c>s.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type requests are mapped from.</typeparam>
public class JsonDefaultMultiSerializerOptionsRequestMapper<TContext> : MultiSerializerOptionsRequestMapper<TContext, JsonSerializer>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDefaultMultiSerializerOptionsRequestMapper{TContext}"/> class.
    /// </summary>
    /// <param name="serviceResolver">Resolver used to obtain the selected serializer, or the default <see cref="JsonSerializer"/>.</param>
    /// <param name="messageBodyGetter">Extracts the raw message body from the context.</param>
    /// <param name="options">The candidate serializer options to evaluate against each context.</param>
    /// <param name="enrichers">The enrichers applied onto every mapped request.</param>
    public JsonDefaultMultiSerializerOptionsRequestMapper(
        IServiceResolver serviceResolver,
        IMessageBodyGetter<TContext> messageBodyGetter,
        IEnumerable<ISerializerOption<TContext>> options,
        IEnumerable<IRequestEnricher<TContext>> enrichers)
        : base(serviceResolver, messageBodyGetter, options, enrichers)
    { }
}
