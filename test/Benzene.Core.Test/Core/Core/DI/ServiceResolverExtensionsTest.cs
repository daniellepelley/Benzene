using Benzene.Abstractions.DI;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Core.Core.DI;

public class ServiceResolverExtensionsTest
{
    private class Foo
    {
    }

    [Fact]
    public void Resolve_RegisteredService_ReturnsService()
    {
        var services = ServiceResolverMother.CreateServiceCollection(x => x.AddScoped<Foo>());
        using var resolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        Assert.NotNull(resolver.Resolve<Foo>());
    }

    [Fact]
    public void TryResolve_UnregisteredService_ReturnsNull()
    {
        var resolver = ServiceResolverMother.CreateServiceResolver();

        Assert.Null(resolver.TryResolve<Foo>());
    }

    [Fact]
    public void TryResolve_RegisteredService_ReturnsService()
    {
        var services = ServiceResolverMother.CreateServiceCollection(x => x.AddScoped<Foo>());
        using var resolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        Assert.NotNull(resolver.TryResolve<Foo>());
    }
}
