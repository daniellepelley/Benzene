using Benzene.DryIoc;
using DryIoc;
using Xunit;

namespace Benzene.DryIoc.Test;

/// <summary>
/// Unit coverage for the DryIoc DI adapter, mirroring the Autofac/Microsoft adapter tests in
/// <c>Benzene.Core.Test</c> (collection resolution, scope disposal, existing-instance registration).
/// Lives in its own project because <c>DryIoc.dll</c> exposes a stray public <c>Example</c> namespace
/// that would collide with <c>Benzene.Core.Test</c>'s own <c>Example</c> type.
/// </summary>
public class DryIocAdapterTest
{
    private interface IWidget { }
    private class WidgetA : IWidget { }
    private class WidgetB : IWidget { }

    private class DisposableTracker : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    private class Marker { }

    [Fact]
    public void GetServices_ReturnsAllRegistrations()
    {
        var container = Extensions.CreateContainer();
        container.Register<IWidget, WidgetA>(Reuse.Singleton);
        container.Register<IWidget, WidgetB>(Reuse.Singleton);

        using var factory = new DryIocServiceResolverFactory(container);
        using var scope = factory.CreateScope();

        var widgets = scope.GetServices<IWidget>().ToArray();

        Assert.Equal(2, widgets.Length);
        Assert.Contains(widgets, x => x is WidgetA);
        Assert.Contains(widgets, x => x is WidgetB);
    }

    [Fact]
    public void GetServices_ReturnsEmptyWhenNoneRegistered()
    {
        var container = Extensions.CreateContainer();

        using var factory = new DryIocServiceResolverFactory(container);
        using var scope = factory.CreateScope();

        Assert.Empty(scope.GetServices<IWidget>());
    }

    [Fact]
    public void DisposingTheScopeResolver_DisposesScopedServices()
    {
        var container = Extensions.CreateContainer();
        container.Register<DisposableTracker>(Reuse.Scoped);

        using var factory = new DryIocServiceResolverFactory(container);
        var scope = factory.CreateScope();
        var tracker = scope.GetService<DisposableTracker>();

        Assert.False(tracker.Disposed);

        scope.Dispose();

        Assert.True(tracker.Disposed);
    }

    [Fact]
    public void AddScopedInstance_ResolvesTheSuppliedInstance()
    {
        var container = Extensions.CreateContainer();
        var instance = new Marker();

        new DryIocBenzeneServiceContainer(container).AddScoped(instance);

        using var factory = new DryIocServiceResolverFactory(container);
        using var scope = factory.CreateScope();

        Assert.Same(instance, scope.GetService<Marker>());
    }

    [Fact]
    public void AddTransientInstance_ResolvesTheSuppliedInstance()
    {
        var container = Extensions.CreateContainer();
        var instance = new Marker();

        new DryIocBenzeneServiceContainer(container).AddTransient(instance);

        using var factory = new DryIocServiceResolverFactory(container);
        using var scope = factory.CreateScope();

        Assert.Same(instance, scope.GetService<Marker>());
    }

    [Fact]
    public void LastRegistrationWins_ForSingleResolve()
    {
        // Benzene relies on this (e.g. overriding a default registration). CreateContainer() configures
        // SelectLastRegisteredFactory; a plain `new Container()` would throw on the second single resolve.
        var container = Extensions.CreateContainer();
        container.RegisterInstance<IWidget>(new WidgetA());
        container.RegisterInstance<IWidget>(new WidgetB());

        using var factory = new DryIocServiceResolverFactory(container);
        using var scope = factory.CreateScope();

        Assert.IsType<WidgetB>(scope.GetService<IWidget>());
    }
}
