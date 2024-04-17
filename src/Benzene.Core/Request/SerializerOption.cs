using System;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.Request;

public class SerializerOption<TContext, TSerializer> : ISerializerOption<TContext> where TSerializer : class, ISerializer
{
    public SerializerOption(Func<TContext, bool> canHandle)
    {
        CanHandle = canHandle;
    }

    public Func<TContext, bool> CanHandle { get; }
    
    public ISerializer GetSerializer(IServiceResolver serviceResolver)
    {
        return serviceResolver.GetService<TSerializer>();
    }
}

public abstract class SerializerOptionBase<TContext, TSerializer> : ISerializerOption<TContext> where TSerializer : class, ISerializer
{
    public abstract ISerializer GetSerializer(IServiceResolver serviceResolver);

    public abstract Func<TContext, bool> CanHandle { get; }
}
