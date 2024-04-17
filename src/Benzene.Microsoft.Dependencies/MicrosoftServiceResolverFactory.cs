using Benzene.Abstractions.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Microsoft.Dependencies;

public class MicrosoftServiceResolverFactory : IServiceResolverFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MicrosoftServiceResolverFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public MicrosoftServiceResolverFactory(IServiceCollection container)
    {
        _serviceProvider = container.BuildServiceProvider();
    }

    public void Dispose()
    {
        //Nothing to dispose
    }

    public IServiceResolver CreateScope()
    {
        return new MicrosoftServiceResolverAdapter(_serviceProvider.CreateScope().ServiceProvider);
    }
}
