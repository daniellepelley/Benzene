using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.MessageHandlers.Request;

/// <summary>
/// Convenience base class for <see cref="ISerializerOption{TContext}"/> implementations that resolve
/// a specific serializer type (<typeparamref name="TSerializer"/>) from DI, leaving only the
/// applicability predicate (<see cref="CanHandle"/>) for subclasses to implement.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this option can apply to.</typeparam>
/// <typeparam name="TSerializer">The serializer type this option resolves and offers.</typeparam>
public abstract class SerializerOptionBase<TContext, TSerializer> : ISerializerOption<TContext> where TSerializer : class, ISerializer
{
    /// <inheritdoc />
    public abstract ISerializer GetSerializer(IServiceResolver serviceResolver);

    /// <inheritdoc />
    public abstract IContextPredicate<TContext> CanHandle { get; }
}
