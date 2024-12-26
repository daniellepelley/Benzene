using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.MessageHandlers.Request;

public interface IContextPredicate<TContext>
{
    bool Check(TContext context, IServiceResolver serviceResolver);
}
