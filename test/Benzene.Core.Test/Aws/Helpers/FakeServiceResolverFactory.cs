using Benzene.Abstractions.DI;

namespace Benzene.Test.Aws.Helpers;

public class FakeServiceResolverFactory : IServiceResolverFactory
{
    private readonly IServiceResolver _serviceResolver;

    public FakeServiceResolverFactory(IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
    }

    public void Dispose()
    {
    }

    public IServiceResolver CreateScope()
    {
        return _serviceResolver;
    }
}