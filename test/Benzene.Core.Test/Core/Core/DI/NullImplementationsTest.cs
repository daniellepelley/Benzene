using System;
using Benzene.Core.Middleware;
using Xunit;

namespace Benzene.Test.Core.Core.DI;

public class NullImplementationsTest
{
    private interface IFoo
    {
    }

    private class Foo : IFoo
    {
    }

    [Fact]
    public void NullServiceResolver_GetService_ReturnsDefault()
    {
        var resolver = new NullServiceResolver();

        Assert.Null(resolver.GetService<Foo>());
    }

    [Fact]
    public void NullServiceResolver_TryGetService_ReturnsNull()
    {
        var resolver = new NullServiceResolver();

        Assert.Null(resolver.TryGetService<Foo>());
    }

    [Fact]
    public void NullServiceResolver_Dispose_DoesNotThrow()
    {
        var resolver = new NullServiceResolver();

        resolver.Dispose();
    }

    [Fact]
    public void NullServiceResolverFactory_CreateScope_ReturnsNullServiceResolver()
    {
        var factory = new NullServiceResolverFactory();

        var scope = factory.CreateScope();

        Assert.IsType<NullServiceResolver>(scope);
    }

    [Fact]
    public void NullServiceResolverFactory_Dispose_DoesNotThrow()
    {
        var factory = new NullServiceResolverFactory();

        factory.Dispose();
    }

    [Fact]
    public void NullBenzeneServiceContainer_IsTypeRegistered_AlwaysTrue()
    {
        var container = new NullBenzeneServiceContainer();

        Assert.True(container.IsTypeRegistered<Foo>());
        Assert.True(container.IsTypeRegistered(typeof(Foo)));
    }

    [Fact]
    public void NullBenzeneServiceContainer_CreateServiceResolverFactory_ReturnsNullServiceResolverFactory()
    {
        var container = new NullBenzeneServiceContainer();

        var factory = container.CreateServiceResolverFactory();

        Assert.IsType<NullServiceResolverFactory>(factory);
    }

    [Fact]
    public void NullBenzeneServiceContainer_AddServiceResolver_ReturnsSameInstance()
    {
        var container = new NullBenzeneServiceContainer();

        var result = container.AddServiceResolver();

        Assert.Same(container, result);
    }

    [Fact]
    public void NullBenzeneServiceContainer_AddMethods_AllThrowNotImplemented()
    {
        var container = new NullBenzeneServiceContainer();

        Assert.Throws<NotImplementedException>(() => container.AddScoped<Foo>());
        Assert.Throws<NotImplementedException>(() => container.AddScoped<IFoo, Foo>());
        Assert.Throws<NotImplementedException>(() => container.AddScoped(typeof(Foo)));
        Assert.Throws<NotImplementedException>(() => container.AddScoped(typeof(IFoo), typeof(Foo)));
        Assert.Throws<NotImplementedException>(() => container.AddScoped(new Foo()));
        Assert.Throws<NotImplementedException>(() => container.AddScoped(_ => new Foo()));

        Assert.Throws<NotImplementedException>(() => container.AddTransient<Foo>());
        Assert.Throws<NotImplementedException>(() => container.AddTransient<IFoo, Foo>());
        Assert.Throws<NotImplementedException>(() => container.AddTransient(typeof(Foo)));
        Assert.Throws<NotImplementedException>(() => container.AddTransient(typeof(IFoo), typeof(Foo)));
        Assert.Throws<NotImplementedException>(() => container.AddTransient(new Foo()));
        Assert.Throws<NotImplementedException>(() => container.AddTransient(_ => new Foo()));

        Assert.Throws<NotImplementedException>(() => container.AddSingleton<Foo>());
        Assert.Throws<NotImplementedException>(() => container.AddSingleton<IFoo, Foo>());
        Assert.Throws<NotImplementedException>(() => container.AddSingleton(typeof(Foo)));
        Assert.Throws<NotImplementedException>(() => container.AddSingleton(typeof(IFoo), typeof(Foo)));
        Assert.Throws<NotImplementedException>(() => container.AddSingleton(new Foo()));
        Assert.Throws<NotImplementedException>(() => container.AddSingleton(_ => new Foo()));
    }
}
