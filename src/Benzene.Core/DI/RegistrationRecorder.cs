using System;
using System.Collections.Generic;
using Benzene.Abstractions.DI;

namespace Benzene.Core.DI;

/// <summary>
/// Records dependency injection registration calls for validation and diagnostic purposes without actually registering services.
/// </summary>
public class RegistrationRecorder : IBenzeneServiceContainer
{
    private readonly List<Type> _types = new();

    /// <summary>
    /// Gets all types that have been recorded through registration calls.
    /// </summary>
    /// <returns>An array of recorded types.</returns>
    public Type[] GetTypes()
    {
        return _types.ToArray();
    }

    /// <summary>
    /// Checks if a type is registered. Always returns false as this is a recording implementation.
    /// </summary>
    /// <typeparam name="TService">The service type to check.</typeparam>
    /// <returns>Always returns false.</returns>
    public bool IsTypeRegistered<TService>()
    {
        return false;
    }

    /// <summary>
    /// Checks if a type is registered. Always returns false as this is a recording implementation.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>Always returns false.</returns>
    public bool IsTypeRegistered(Type type)
    {
        return false;
    }

    /// <summary>
    /// Records a scoped service registration.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddScoped<TImplementation>() where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    /// <summary>
    /// Records a scoped service registration with service and implementation types.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        _types.Add(typeof(TService));
        return this;
    }

    /// <summary>
    /// Records a scoped service registration by type.
    /// </summary>
    /// <param name="type">The type to record.</param>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddScoped(Type type)
    {
        _types.Add(type);
        return this;
    }

    /// <summary>
    /// Records a scoped service registration with service and implementation types.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddScoped(Type serviceType, Type implementationType)
    {
        _types.Add(serviceType);
        return this;
    }

    /// <summary>
    /// Records a scoped service registration with an instance.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="implementation">The implementation instance.</param>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddScoped<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    /// <summary>
    /// Records a scoped service registration with a factory function.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="func">The factory function.</param>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddScoped<TImplementation>(Func<IServiceResolver, TImplementation> func) where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    /// <summary>
    /// Records a transient service registration.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddTransient<TImplementation>() where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    /// <summary>
    /// Records a transient service registration with service and implementation types.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        _types.Add(typeof(TService));
        return this;
    }

    /// <summary>
    /// Records a transient service registration by type.
    /// </summary>
    /// <param name="type">The type to record.</param>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddTransient(Type type)
    {
        _types.Add(type);
        return this;
    }

    /// <summary>
    /// Records a transient service registration with service and implementation types.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddTransient(Type serviceType, Type implementationType)
    {
        _types.Add(serviceType);
        return this;
    }

    /// <summary>
    /// Records a transient service registration with an instance.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="implementation">The implementation instance.</param>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddTransient<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    /// <summary>
    /// Records a transient service registration with a factory function.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="func">The factory function.</param>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddTransient<TImplementation>(Func<IServiceResolver, TImplementation> func) where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    /// <summary>
    /// Records a singleton service registration.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddSingleton<TImplementation>() where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    /// <summary>
    /// Records a singleton service registration with service and implementation types.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        _types.Add(typeof(TService));
        return this;
    }

    /// <summary>
    /// Records a singleton service registration by type.
    /// </summary>
    /// <param name="type">The type to record.</param>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddSingleton(Type type)
    {
        _types.Add(type);
        return this;
    }

    /// <summary>
    /// Records a singleton service registration with service and implementation types.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddSingleton(Type serviceType, Type implementationType)
    {
        _types.Add(serviceType);
        return this;
    }

    /// <summary>
    /// Records a singleton service registration with an instance.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="implementation">The implementation instance.</param>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddSingleton<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    /// <summary>
    /// Records a singleton service registration with a factory function.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="func">The factory function.</param>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddSingleton<TImplementation>(Func<IServiceResolver, TImplementation> func) where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    /// <summary>
    /// Creates a service resolver factory. Returns null as this is a recording implementation.
    /// </summary>
    /// <returns>Always returns null.</returns>
    public IServiceResolverFactory CreateServiceResolverFactory()
    {
        return null;
    }

    /// <summary>
    /// Records a service resolver registration.
    /// </summary>
    /// <returns>The recorder for method chaining.</returns>
    public IBenzeneServiceContainer AddServiceResolver()
    {
        return this;
    }
}
