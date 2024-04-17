using Benzene.Abstractions.DI;

namespace Benzene.Abstractions;

public interface IDependencyWrapper<T>
{
    T Wrap(IServiceResolver serviceResolver, T source);
}
