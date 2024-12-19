using System;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Serialization;
using Benzene.Core.Predicates;

namespace Benzene.Core.Request;

public class InlineSerializerOption<TContext> : ISerializerOption<TContext> 
{
    private readonly ISerializer _serializer;

    public InlineSerializerOption(Func<TContext, bool> canHandle, ISerializer serializer)
        :this((context, _) => canHandle(context), serializer)
    { }

    public InlineSerializerOption(Func<TContext, IServiceResolver, bool> canHandle, ISerializer serializer)
    {
        _serializer = serializer;
        CanHandle = new InlineContextPredicate<TContext>(canHandle);
    }

    public IContextPredicate<TContext> CanHandle { get; }
    
    public ISerializer GetSerializer(IServiceResolver serviceResolver)
    {
        return _serializer;
    }
}
