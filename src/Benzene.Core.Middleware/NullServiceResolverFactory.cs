using Benzene.Abstractions.DI;

namespace Benzene.Core.Middleware;

public class NullServiceResolverFactory : IServiceResolverFactory
{
    public void Dispose()
    {
    }

    public IServiceResolver CreateScope()
    {
        return new NullServiceResolver();

    }
}