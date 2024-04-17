using Benzene.Abstractions;
using Benzene.Abstractions.DI;

namespace Benzene.Clients;

public class ClientBuilder
{
    private readonly List<IDependencyWrapper<IBenzeneMessageClient>> _dependencyWrappers = new();
    private readonly Func<IServiceResolver, IBenzeneMessageClient> _builder;

    public ClientBuilder(Func<IServiceResolver, IBenzeneMessageClient> builder)
    {
        _builder = builder;
    }
        
    public ClientBuilder WithDependencyWrapper(IDependencyWrapper<IBenzeneMessageClient> dependencyWrapper)
    {
        _dependencyWrappers.Add(dependencyWrapper);
        return this;
    }

    public IBenzeneMessageClient Build(IServiceResolver serviceResolver)
    {
        var factory = new DependencyWrapperFactory<IBenzeneMessageClient>(_dependencyWrappers);
        return factory.Create(serviceResolver, _builder(serviceResolver));
    }
}
