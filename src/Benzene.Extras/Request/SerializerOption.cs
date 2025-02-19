using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Core.Messages.Predicates;

namespace Benzene.Extras.Request;

public class SerializerOption<TContext, TSerializer> : ISerializerOption<TContext> where TSerializer : class, ISerializer
{
    public SerializerOption(Func<ContextPredicateBuilder<TContext>, IContextPredicate<TContext>> builder)
    {
        CanHandle = builder(new ContextPredicateBuilder<TContext>());
    }

    public SerializerOption(Func<TContext, IServiceResolver, bool> canHandle)
    {
        CanHandle = new InlineContextPredicate<TContext>(canHandle);
    }

    public SerializerOption(IContextPredicate<TContext> canHandle)
    {
        CanHandle = canHandle;
    }

    public IContextPredicate<TContext> CanHandle { get; }
    
    public ISerializer GetSerializer(IServiceResolver serviceResolver)
    {
        return serviceResolver.GetService<TSerializer>();
    }
}