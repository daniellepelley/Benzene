using System.Linq;
using Autofac;
using Benzene.Autofac;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Core.DI;

/// <summary>
/// Coverage for <see cref="Benzene.Abstractions.DI.IServiceResolver.GetServices{T}"/>: resolving
/// all registrations of a service type, which underpins collecting the registered
/// schema casters in Benzene.Core.Versioning.
/// </summary>
public class ServiceResolverGetServicesTest
{
    private interface IWidget
    {
    }

    private class WidgetA : IWidget
    {
    }

    private class WidgetB : IWidget
    {
    }

    [Fact]
    public void Microsoft_GetServices_ReturnsAllRegistrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWidget, WidgetA>();
        services.AddSingleton<IWidget, WidgetB>();

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();

        var widgets = scope.GetServices<IWidget>().ToArray();

        Assert.Equal(2, widgets.Length);
        Assert.Contains(widgets, x => x is WidgetA);
        Assert.Contains(widgets, x => x is WidgetB);
    }

    [Fact]
    public void Microsoft_GetServices_ReturnsEmptyWhenNoneRegistered()
    {
        var services = new ServiceCollection();

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();

        Assert.Empty(scope.GetServices<IWidget>());
    }

    [Fact]
    public void Autofac_GetServices_ReturnsAllRegistrations()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<WidgetA>().As<IWidget>().SingleInstance();
        containerBuilder.RegisterType<WidgetB>().As<IWidget>().SingleInstance();

        using var factory = new AutofacServiceResolverFactory(containerBuilder);
        using var scope = factory.CreateScope();

        var widgets = scope.GetServices<IWidget>().ToArray();

        Assert.Equal(2, widgets.Length);
        Assert.Contains(widgets, x => x is WidgetA);
        Assert.Contains(widgets, x => x is WidgetB);
    }

    [Fact]
    public void Autofac_GetServices_ReturnsEmptyWhenNoneRegistered()
    {
        var containerBuilder = new ContainerBuilder();

        using var factory = new AutofacServiceResolverFactory(containerBuilder);
        using var scope = factory.CreateScope();

        Assert.Empty(scope.GetServices<IWidget>());
    }

    [Fact]
    public void NullServiceResolver_GetServices_ReturnsEmpty()
    {
        using var resolver = new NullServiceResolver();

        Assert.Empty(resolver.GetServices<IWidget>());
    }
}
