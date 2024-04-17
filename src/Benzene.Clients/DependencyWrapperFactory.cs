using Benzene.Abstractions;
using Benzene.Abstractions.DI;

namespace Benzene.Clients;

public class DependencyWrapperFactory<T>
{
    private readonly IEnumerable<IDependencyWrapper<T>> _dependencyWrappers;

    public DependencyWrapperFactory(IEnumerable<IDependencyWrapper<T>> dependencyWrappers)
    {
        _dependencyWrappers = dependencyWrappers;
    }

    public T Create(IServiceResolver serviceResolver, T source)
    {
        return _dependencyWrappers.Aggregate(source, (m, wrapper) => wrapper.Wrap(serviceResolver, m));
    }
}
