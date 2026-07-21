using Autofac;
using Benzene.Autofac;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Core.DI;

/// <summary>
/// The <c>AddScoped(instance)</c> / <c>AddTransient(instance)</c> container overloads register an
/// existing object, so resolving the service must hand back that exact instance (like the singleton
/// instance overload does) rather than constructing a fresh one. The Autofac adapter previously
/// ignored the supplied instance and called <c>RegisterType</c>, diverging from both the documented
/// contract and the Microsoft adapter.
/// </summary>
public class ServiceContainerInstanceRegistrationTest
{
    private class Marker
    {
    }

    [Fact]
    public void Autofac_AddScopedInstance_ResolvesTheSuppliedInstance()
    {
        var builder = new ContainerBuilder();
        var instance = new Marker();

        new AutofacBenzeneServiceContainer(builder).AddScoped(instance);

        using var factory = new AutofacServiceResolverFactory(builder);
        using var scope = factory.CreateScope();

        Assert.Same(instance, scope.GetService<Marker>());
    }

    [Fact]
    public void Autofac_AddTransientInstance_ResolvesTheSuppliedInstance()
    {
        var builder = new ContainerBuilder();
        var instance = new Marker();

        new AutofacBenzeneServiceContainer(builder).AddTransient(instance);

        using var factory = new AutofacServiceResolverFactory(builder);
        using var scope = factory.CreateScope();

        Assert.Same(instance, scope.GetService<Marker>());
    }

    [Fact]
    public void Microsoft_AddScopedInstance_ResolvesTheSuppliedInstance()
    {
        var services = new ServiceCollection();
        var instance = new Marker();

        new MicrosoftBenzeneServiceContainer(services).AddScoped(instance);

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();

        Assert.Same(instance, scope.GetService<Marker>());
    }

    [Fact]
    public void Microsoft_AddTransientInstance_ResolvesTheSuppliedInstance()
    {
        var services = new ServiceCollection();
        var instance = new Marker();

        new MicrosoftBenzeneServiceContainer(services).AddTransient(instance);

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();

        Assert.Same(instance, scope.GetService<Marker>());
    }
}
