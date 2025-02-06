using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Messages;

public interface IContextPredicate<TContext>
{
    bool Check(TContext context, IServiceResolver serviceResolver);
}
