using System;
using Benzene.Abstractions.DI;

namespace Benzene.Core.DI;

public static class BenzeneServiceContainerExtensions
{
    public static IBenzeneServiceContainer TryAddScoped<TImplementation>(this IBenzeneServiceContainer source)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddScoped<TImplementation>();
    }

    public static IBenzeneServiceContainer TryAddScoped<TService, TImplementation>(
        this IBenzeneServiceContainer source)
        where TService : class
        where TImplementation : class, TService
    {
        return source.IsTypeRegistered<TService>()
            ? source
            : source.AddScoped<TService, TImplementation>();
    }
    
    public static IBenzeneServiceContainer TryAddScoped(
        this IBenzeneServiceContainer source, Type serviceType, Type implementationType)
    {
        return source.IsTypeRegistered(serviceType)
            ? source
            : source.AddScoped(serviceType, implementationType);
    }

    public static IBenzeneServiceContainer TryAddScoped(this IBenzeneServiceContainer source, Type type)
    {
        return source.IsTypeRegistered(type)
            ? source
            : source.AddScoped(type);
    }
    
    public static IBenzeneServiceContainer TryAddScoped<TImplementation>(this IBenzeneServiceContainer source, Func<IServiceResolver, TImplementation> func)
        where TImplementation: class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddScoped(func);
    }

    public static IBenzeneServiceContainer AddScoped<TImplementation>(this IBenzeneServiceContainer source, TImplementation implementation)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddScoped(implementation);
    }
   
    public static IBenzeneServiceContainer TryAddTransient<TImplementation>(this IBenzeneServiceContainer source)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddTransient<TImplementation>();
    }

    public static IBenzeneServiceContainer TryAddTransient<TService, TImplementation>(
        this IBenzeneServiceContainer source)
        where TService : class
        where TImplementation : class, TService
    {
        return source.IsTypeRegistered<TService>()
            ? source
            : source.AddTransient<TService, TImplementation>();
    }
    
    public static IBenzeneServiceContainer TryAddTransient(
        this IBenzeneServiceContainer source, Type serviceType, Type implementationType)
    {
        return source.IsTypeRegistered(serviceType)
            ? source
            : source.AddTransient(serviceType, implementationType);
    }

    public static IBenzeneServiceContainer TryAddTransient(this IBenzeneServiceContainer source, Type type)
    {
        return source.IsTypeRegistered(type)
            ? source
            : source.AddTransient(type);
    }
    
    public static IBenzeneServiceContainer TryAddTransient<TImplementation>(this IBenzeneServiceContainer source, Func<IServiceResolver, TImplementation> func)
        where TImplementation: class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddTransient(func);
    }

    public static IBenzeneServiceContainer TryAddTransient<TImplementation>(this IBenzeneServiceContainer source, TImplementation implementation)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddTransient(implementation);
    }
    
    public static IBenzeneServiceContainer TryAddSingleton<TImplementation>(this IBenzeneServiceContainer source)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddSingleton<TImplementation>();
    }
    
    public static IBenzeneServiceContainer TryAddSingleton<TService, TImplementation>(this IBenzeneServiceContainer source)
        where TService : class
        where TImplementation : class, TService
    {
        return source.IsTypeRegistered<TService>()
            ? source
            : source.AddSingleton<TService, TImplementation>();
    }
    
    public static IBenzeneServiceContainer TryAddSingleton(this IBenzeneServiceContainer source,Type type)
    {
        return source.IsTypeRegistered(type)
            ? source
            : source.AddScoped(type);
    }

    public static IBenzeneServiceContainer TryAddSingleton<TImplementation>(this IBenzeneServiceContainer source,TImplementation implementation)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddSingleton(implementation);
    }
    
    public static IBenzeneServiceContainer TryAddSingleton<TImplementation>(this IBenzeneServiceContainer source,Func<IServiceResolver, TImplementation> func)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddSingleton(func);
    }

    public static IBenzeneServiceContainer TryAddSingleton(this IBenzeneServiceContainer source, Type serviceType, Type implementationType)
    {
        return source.IsTypeRegistered(serviceType)
            ? source
            : source.AddSingleton(serviceType, implementationType);
    }
}
