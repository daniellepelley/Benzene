using Benzene.Abstractions.DI;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.Request;

public abstract class SerializerOptionBase<TContext, TSerializer> : ISerializerOption<TContext> where TSerializer : class, ISerializer
{
    public abstract ISerializer GetSerializer(IServiceResolver serviceResolver);

    public abstract IContextPredicate<TContext> CanHandle { get; }
}