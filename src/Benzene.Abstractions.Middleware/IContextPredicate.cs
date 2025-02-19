using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Middleware;

public interface IContextPredicate<TContext>
{
    bool Check(TContext context, IServiceResolver serviceResolver);
}
