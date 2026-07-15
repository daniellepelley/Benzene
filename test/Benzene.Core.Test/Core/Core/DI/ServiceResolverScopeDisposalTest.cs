using System;
using Autofac;
using Benzene.Abstractions.DI;
using Benzene.Autofac;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Core.DI;

/// <summary>
/// Regression coverage for a fix to both DI adapters: <see cref="IServiceResolverFactory.CreateScope"/>
/// creates a real underlying DI scope (an <see cref="Microsoft.Extensions.DependencyInjection.IServiceScope"/>
/// or Autofac <see cref="ILifetimeScope"/>), and disposing the returned <c>IServiceResolver</c> must
/// actually dispose that scope so its scoped <see cref="IDisposable"/> services are released. Previously
/// both adapters' <c>Dispose()</c> was a no-op, silently leaking a scope on every
/// request/message/batch-record.
/// </summary>
public class ServiceResolverScopeDisposalTest
{
    private class DisposableTracker : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    [Fact]
    public void Microsoft_DisposingTheScopeResolver_DisposesScopedServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<DisposableTracker>();

        using var factory = new MicrosoftServiceResolverFactory(services);
        var scope = factory.CreateScope();
        var tracker = scope.GetService<DisposableTracker>();

        Assert.False(tracker.Disposed);

        scope.Dispose();

        Assert.True(tracker.Disposed);
    }

    [Fact]
    public void Autofac_DisposingTheScopeResolver_DisposesScopedServices()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<DisposableTracker>().InstancePerLifetimeScope();

        using var factory = new AutofacServiceResolverFactory(containerBuilder);
        var scope = factory.CreateScope();
        var tracker = scope.GetService<DisposableTracker>();

        Assert.False(tracker.Disposed);

        scope.Dispose();

        Assert.True(tracker.Disposed);
    }
}
