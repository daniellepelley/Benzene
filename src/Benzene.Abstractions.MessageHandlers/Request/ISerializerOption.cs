using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;

namespace Benzene.Abstractions.MessageHandlers.Request;

/// <summary>
/// One candidate serializer a request mapper can choose for a given context, paired with a
/// predicate deciding whether it applies (e.g. based on a content-type header). A request mapper
/// that supports multiple serializers evaluates its registered options against the incoming
/// context and uses the first one whose <see cref="CanHandle"/> matches.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this option can apply to.</typeparam>
public interface ISerializerOption<TContext>
{
    /// <summary>Predicate deciding whether this option's serializer applies to a given context.</summary>
    IContextPredicate<TContext> CanHandle { get; }

    /// <summary>Resolves the serializer to use when this option is selected.</summary>
    /// <param name="serviceResolver">Resolver used to obtain the serializer instance.</param>
    /// <returns>The serializer for this option.</returns>
    ISerializer GetSerializer(IServiceResolver serviceResolver);
}