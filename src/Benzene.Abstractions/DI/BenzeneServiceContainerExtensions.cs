namespace Benzene.Abstractions.DI;

/// <summary>
/// Provides extension methods for IBenzeneServiceContainer that add conditional registration capabilities.
/// These "Try" methods only register a service if it is not already registered, preventing duplicate registrations.
/// </summary>
public static class BenzeneServiceContainerExtensions
{
    /// <summary>
    /// Registers a scoped service if it is not already registered.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type to register.</typeparam>
    /// <param name="source">The service container.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddScoped<TImplementation>(this IBenzeneServiceContainer source)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddScoped<TImplementation>();
    }

    /// <summary>
    /// Registers a scoped service with separate service and implementation types if the service type is not already registered.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="source">The service container.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddScoped<TService, TImplementation>(
        this IBenzeneServiceContainer source)
        where TService : class
        where TImplementation : class, TService
    {
        return source.IsTypeRegistered<TService>()
            ? source
            : source.AddScoped<TService, TImplementation>();
    }

    /// <summary>
    /// Registers a scoped service with separate service and implementation types using runtime type information if the service type is not already registered.
    /// </summary>
    /// <param name="source">The service container.</param>
    /// <param name="serviceType">The service type to register.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddScoped(
        this IBenzeneServiceContainer source, Type serviceType, Type implementationType)
    {
        return source.IsTypeRegistered(serviceType)
            ? source
            : source.AddScoped(serviceType, implementationType);
    }

    /// <summary>
    /// Registers a scoped service using runtime type information if it is not already registered.
    /// </summary>
    /// <param name="source">The service container.</param>
    /// <param name="type">The type to register as both service and implementation.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddScoped(this IBenzeneServiceContainer source, Type type)
    {
        return source.IsTypeRegistered(type)
            ? source
            : source.AddScoped(type);
    }

    /// <summary>
    /// Registers a scoped service using a factory function if it is not already registered.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="source">The service container.</param>
    /// <param name="func">The factory function that creates the service instance.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddScoped<TImplementation>(this IBenzeneServiceContainer source, Func<IServiceResolver, TImplementation> func)
        where TImplementation: class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddScoped(func);
    }

    /// <summary>
    /// Registers a scoped service using an existing instance if it is not already registered.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="source">The service container.</param>
    /// <param name="implementation">The instance to register.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddScoped<TImplementation>(this IBenzeneServiceContainer source, TImplementation implementation)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddScoped(implementation);
    }
   
    /// <summary>
    /// Registers a transient service if it is not already registered.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type to register.</typeparam>
    /// <param name="source">The service container.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddTransient<TImplementation>(this IBenzeneServiceContainer source)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddTransient<TImplementation>();
    }

    /// <summary>
    /// Registers a transient service with separate service and implementation types if the service type is not already registered.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="source">The service container.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddTransient<TService, TImplementation>(
        this IBenzeneServiceContainer source)
        where TService : class
        where TImplementation : class, TService
    {
        return source.IsTypeRegistered<TService>()
            ? source
            : source.AddTransient<TService, TImplementation>();
    }

    /// <summary>
    /// Registers a transient service with separate service and implementation types using runtime type information if the service type is not already registered.
    /// </summary>
    /// <param name="source">The service container.</param>
    /// <param name="serviceType">The service type to register.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddTransient(
        this IBenzeneServiceContainer source, Type serviceType, Type implementationType)
    {
        return source.IsTypeRegistered(serviceType)
            ? source
            : source.AddTransient(serviceType, implementationType);
    }

    /// <summary>
    /// Registers a transient service using runtime type information if it is not already registered.
    /// </summary>
    /// <param name="source">The service container.</param>
    /// <param name="type">The type to register as both service and implementation.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddTransient(this IBenzeneServiceContainer source, Type type)
    {
        return source.IsTypeRegistered(type)
            ? source
            : source.AddTransient(type);
    }

    /// <summary>
    /// Registers a transient service using a factory function if it is not already registered.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="source">The service container.</param>
    /// <param name="func">The factory function that creates the service instance.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddTransient<TImplementation>(this IBenzeneServiceContainer source, Func<IServiceResolver, TImplementation> func)
        where TImplementation: class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddTransient(func);
    }

    /// <summary>
    /// Registers a transient service using an existing instance if it is not already registered.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="source">The service container.</param>
    /// <param name="implementation">The instance to register.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddTransient<TImplementation>(this IBenzeneServiceContainer source, TImplementation implementation)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddTransient(implementation);
    }
    
    /// <summary>
    /// Registers a singleton service if it is not already registered.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type to register.</typeparam>
    /// <param name="source">The service container.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddSingleton<TImplementation>(this IBenzeneServiceContainer source)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddSingleton<TImplementation>();
    }

    /// <summary>
    /// Registers a singleton service with separate service and implementation types if the service type is not already registered.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="source">The service container.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddSingleton<TService, TImplementation>(this IBenzeneServiceContainer source)
        where TService : class
        where TImplementation : class, TService
    {
        return source.IsTypeRegistered<TService>()
            ? source
            : source.AddSingleton<TService, TImplementation>();
    }

    /// <summary>
    /// Registers a singleton service using runtime type information if it is not already registered.
    /// </summary>
    /// <param name="source">The service container.</param>
    /// <param name="type">The type to register as both service and implementation.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddSingleton(this IBenzeneServiceContainer source,Type type)
    {
        return source.IsTypeRegistered(type)
            ? source
            : source.AddSingleton(type);
    }

    /// <summary>
    /// Registers a singleton service using an existing instance if it is not already registered.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="source">The service container.</param>
    /// <param name="implementation">The instance to register.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddSingleton<TImplementation>(this IBenzeneServiceContainer source,TImplementation implementation)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddSingleton(implementation);
    }

    /// <summary>
    /// Registers a singleton service using a factory function if it is not already registered.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="source">The service container.</param>
    /// <param name="func">The factory function that creates the service instance.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddSingleton<TImplementation>(this IBenzeneServiceContainer source,Func<IServiceResolver, TImplementation> func)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddSingleton(func);
    }

    /// <summary>
    /// Registers a singleton service with separate service and implementation types using runtime type information if the service type is not already registered.
    /// </summary>
    /// <param name="source">The service container.</param>
    /// <param name="serviceType">The service type to register.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer TryAddSingleton(this IBenzeneServiceContainer source, Type serviceType, Type implementationType)
    {
        return source.IsTypeRegistered(serviceType)
            ? source
            : source.AddSingleton(serviceType, implementationType);
    }
}
