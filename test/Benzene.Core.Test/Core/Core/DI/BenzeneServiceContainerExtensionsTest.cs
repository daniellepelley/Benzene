using System.Linq;
using Benzene.Abstractions.DI;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Core.DI;

public class BenzeneServiceContainerExtensionsTest
{
    private interface IFoo
    {
    }

    private class Foo : IFoo
    {
    }

    private class OtherFoo : IFoo
    {
    }

    private static ServiceLifetime LifetimeOf<TService>(ServiceCollection services)
    {
        return services.Single(x => x.ServiceType == typeof(TService)).Lifetime;
    }

    [Fact]
    public void TryAddScoped_RegistersWhenAbsent()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddScoped<Foo>();

        Assert.True(container.IsTypeRegistered<Foo>());
        Assert.Equal(ServiceLifetime.Scoped, LifetimeOf<Foo>(services));
    }

    [Fact]
    public void TryAddScoped_DoesNotDuplicateWhenAlreadyRegistered()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.AddScoped<Foo>();
        container.TryAddScoped<Foo>();

        Assert.Single(services, x => x.ServiceType == typeof(Foo));
    }

    [Fact]
    public void TryAddScoped_ServiceAndImplementation_RegistersWhenAbsent()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddScoped<IFoo, Foo>();

        Assert.True(container.IsTypeRegistered<IFoo>());
        Assert.Equal(ServiceLifetime.Scoped, LifetimeOf<IFoo>(services));
    }

    [Fact]
    public void TryAddScoped_ServiceAndImplementation_DoesNotDuplicateWhenAlreadyRegistered()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.AddScoped<IFoo, Foo>();
        container.TryAddScoped<IFoo, OtherFoo>();

        var descriptor = Assert.Single(services, x => x.ServiceType == typeof(IFoo));
        Assert.Equal(typeof(Foo), descriptor.ImplementationType);
    }

    [Fact]
    public void TryAddScoped_RuntimeType_RegistersWhenAbsent()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddScoped(typeof(Foo));

        Assert.True(container.IsTypeRegistered(typeof(Foo)));
        Assert.Equal(ServiceLifetime.Scoped, LifetimeOf<Foo>(services));
    }

    [Fact]
    public void TryAddScoped_RuntimeServiceAndImplementationType_RegistersWhenAbsent()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddScoped(typeof(IFoo), typeof(Foo));

        Assert.True(container.IsTypeRegistered(typeof(IFoo)));
        Assert.Equal(ServiceLifetime.Scoped, LifetimeOf<IFoo>(services));
    }

    [Fact]
    public void TryAddScoped_RuntimeServiceAndImplementationType_DoesNotDuplicateWhenAlreadyRegistered()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddScoped(typeof(IFoo), typeof(Foo));
        container.TryAddScoped(typeof(IFoo), typeof(OtherFoo));

        var descriptor = Assert.Single(services, x => x.ServiceType == typeof(IFoo));
        Assert.Equal(typeof(Foo), descriptor.ImplementationType);
    }

    [Fact]
    public void TryAddScoped_Factory_RegistersWhenAbsent()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddScoped(_ => new Foo());

        Assert.True(container.IsTypeRegistered<Foo>());
        Assert.Equal(ServiceLifetime.Scoped, LifetimeOf<Foo>(services));
    }

    [Fact]
    public void AddScoped_Instance_RegistersWhenAbsent()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        // Called via the static class explicitly: IBenzeneServiceContainer also declares
        // its own AddScoped<T>(T implementation) member, so normal instance-method syntax
        // always resolves to that (unconditional) method instead of this "Try" extension.
        BenzeneServiceContainerExtensions.AddScoped(container, new Foo());

        Assert.True(container.IsTypeRegistered<Foo>());
    }

    [Fact]
    public void AddScoped_Instance_DoesNotDuplicateWhenAlreadyRegistered()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        BenzeneServiceContainerExtensions.AddScoped(container, new Foo());
        BenzeneServiceContainerExtensions.AddScoped(container, new Foo());

        Assert.Single(services, x => x.ServiceType == typeof(Foo));
    }

    [Fact]
    public void TryAddTransient_RegistersWhenAbsent()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddTransient<Foo>();

        Assert.True(container.IsTypeRegistered<Foo>());
        Assert.Equal(ServiceLifetime.Transient, LifetimeOf<Foo>(services));
    }

    [Fact]
    public void TryAddTransient_DoesNotDuplicateWhenAlreadyRegistered()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.AddTransient<Foo>();
        container.TryAddTransient<Foo>();

        Assert.Single(services, x => x.ServiceType == typeof(Foo));
    }

    [Fact]
    public void TryAddTransient_ServiceAndImplementation_RegistersWhenAbsent()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddTransient<IFoo, Foo>();

        Assert.True(container.IsTypeRegistered<IFoo>());
        Assert.Equal(ServiceLifetime.Transient, LifetimeOf<IFoo>(services));
    }

    [Fact]
    public void TryAddTransient_RuntimeType_RegistersWhenAbsent()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddTransient(typeof(Foo));

        Assert.True(container.IsTypeRegistered(typeof(Foo)));
        Assert.Equal(ServiceLifetime.Transient, LifetimeOf<Foo>(services));
    }

    [Fact]
    public void TryAddTransient_RuntimeServiceAndImplementationType_RegistersWhenAbsent()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddTransient(typeof(IFoo), typeof(Foo));

        Assert.True(container.IsTypeRegistered(typeof(IFoo)));
        Assert.Equal(ServiceLifetime.Transient, LifetimeOf<IFoo>(services));
    }

    [Fact]
    public void TryAddTransient_Factory_RegistersWhenAbsent()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddTransient(_ => new Foo());

        Assert.True(container.IsTypeRegistered<Foo>());
        Assert.Equal(ServiceLifetime.Transient, LifetimeOf<Foo>(services));
    }

    [Fact]
    public void TryAddTransient_Instance_RegistersWhenAbsent()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddTransient(new Foo());

        Assert.True(container.IsTypeRegistered<Foo>());
    }

    [Fact]
    public void TryAddTransient_Instance_DoesNotDuplicateWhenAlreadyRegistered()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.AddTransient<Foo>();
        container.TryAddTransient(new Foo());

        Assert.Single(services, x => x.ServiceType == typeof(Foo));
    }

    // Regression test: TryAddSingleton previously called AddScoped internally
    // (fixed in BenzeneServiceContainerExtensions.cs), which meant "singleton"
    // registrations silently behaved as scoped.
    [Fact]
    public void TryAddSingleton_RegistersAsSingletonLifetime_NotScoped()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddSingleton<Foo>();

        Assert.True(container.IsTypeRegistered<Foo>());
        Assert.Equal(ServiceLifetime.Singleton, LifetimeOf<Foo>(services));
    }

    [Fact]
    public void TryAddSingleton_DoesNotDuplicateWhenAlreadyRegistered()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.AddSingleton<Foo>();
        container.TryAddSingleton<Foo>();

        Assert.Single(services, x => x.ServiceType == typeof(Foo));
    }

    [Fact]
    public void TryAddSingleton_ServiceAndImplementation_RegistersAsSingletonLifetime()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddSingleton<IFoo, Foo>();

        Assert.True(container.IsTypeRegistered<IFoo>());
        Assert.Equal(ServiceLifetime.Singleton, LifetimeOf<IFoo>(services));
    }

    [Fact]
    public void TryAddSingleton_ServiceAndImplementation_DoesNotDuplicateWhenAlreadyRegistered()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.AddSingleton<IFoo, Foo>();
        container.TryAddSingleton<IFoo, OtherFoo>();

        var descriptor = Assert.Single(services, x => x.ServiceType == typeof(IFoo));
        Assert.Equal(typeof(Foo), descriptor.ImplementationType);
    }

    [Fact]
    public void TryAddSingleton_RuntimeType_RegistersAsSingletonLifetime()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddSingleton(typeof(Foo));

        Assert.True(container.IsTypeRegistered(typeof(Foo)));
        Assert.Equal(ServiceLifetime.Singleton, LifetimeOf<Foo>(services));
    }

    [Fact]
    public void TryAddSingleton_RuntimeServiceAndImplementationType_RegistersAsSingletonLifetime()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddSingleton(typeof(IFoo), typeof(Foo));

        Assert.True(container.IsTypeRegistered(typeof(IFoo)));
        Assert.Equal(ServiceLifetime.Singleton, LifetimeOf<IFoo>(services));
    }

    [Fact]
    public void TryAddSingleton_RuntimeServiceAndImplementationType_DoesNotDuplicateWhenAlreadyRegistered()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddSingleton(typeof(IFoo), typeof(Foo));
        container.TryAddSingleton(typeof(IFoo), typeof(OtherFoo));

        var descriptor = Assert.Single(services, x => x.ServiceType == typeof(IFoo));
        Assert.Equal(typeof(Foo), descriptor.ImplementationType);
    }

    [Fact]
    public void TryAddSingleton_Instance_RegistersWhenAbsent()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var instance = new Foo();

        container.TryAddSingleton(instance);

        Assert.True(container.IsTypeRegistered<Foo>());
        Assert.Equal(ServiceLifetime.Singleton, LifetimeOf<Foo>(services));
    }

    [Fact]
    public void TryAddSingleton_Instance_DoesNotDuplicateWhenAlreadyRegistered()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.AddSingleton(new Foo());
        container.TryAddSingleton(new Foo());

        Assert.Single(services, x => x.ServiceType == typeof(Foo));
    }

    [Fact]
    public void TryAddSingleton_Factory_RegistersAsSingletonLifetime()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.TryAddSingleton(_ => new Foo());

        Assert.True(container.IsTypeRegistered<Foo>());
        Assert.Equal(ServiceLifetime.Singleton, LifetimeOf<Foo>(services));
    }

    [Fact]
    public void TryAddSingleton_Factory_DoesNotDuplicateWhenAlreadyRegistered()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.AddSingleton<Foo>();
        container.TryAddSingleton(_ => new Foo());

        Assert.Single(services, x => x.ServiceType == typeof(Foo));
    }
}
