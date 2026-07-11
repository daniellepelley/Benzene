namespace Benzene.Abstractions.DI;

/// <summary>
/// Provides a DI container abstraction for registering dependencies with different lifetimes.
/// This interface is DI container-agnostic, allowing Benzene to work with any underlying container implementation.
/// </summary>
public interface IBenzeneServiceContainer
{
    /// <summary>
    /// Checks whether a service type is already registered in the container.
    /// </summary>
    /// <typeparam name="TService">The service type to check.</typeparam>
    /// <returns>True if the service type is registered; otherwise, false.</returns>
    bool IsTypeRegistered<TService>();

    /// <summary>
    /// Checks whether a service type is already registered in the container.
    /// </summary>
    /// <param name="type">The service type to check.</param>
    /// <returns>True if the service type is registered; otherwise, false.</returns>
    bool IsTypeRegistered(Type type);

    /// <summary>
    /// Registers a scoped service. A new instance is created once per scope.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type to register.</typeparam>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddScoped<TImplementation>()
        where TImplementation : class;

    /// <summary>
    /// Registers a scoped service with separate service and implementation types. A new instance is created once per scope.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService;

    /// <summary>
    /// Registers a scoped service using runtime type information. A new instance is created once per scope.
    /// </summary>
    /// <param name="type">The type to register as both service and implementation.</param>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddScoped(Type type);

    /// <summary>
    /// Registers a scoped service with separate service and implementation types using runtime type information. A new instance is created once per scope.
    /// </summary>
    /// <param name="serviceType">The service type to register.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddScoped(Type serviceType, Type implementationType);

    /// <summary>
    /// Registers a scoped service using an existing instance. A new instance is created once per scope.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="implementation">The instance to register.</param>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddScoped<TImplementation>(TImplementation implementation)
        where TImplementation : class;

    /// <summary>
    /// Registers a scoped service using a factory function. A new instance is created once per scope.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="func">The factory function that creates the service instance.</param>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddScoped<TImplementation>(Func<IServiceResolver, TImplementation> func)
        where TImplementation: class;

    /// <summary>
    /// Registers a transient service. A new instance is created each time the service is requested.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type to register.</typeparam>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddTransient<TImplementation>()
        where TImplementation : class;

    /// <summary>
    /// Registers a transient service with separate service and implementation types. A new instance is created each time the service is requested.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService;

    /// <summary>
    /// Registers a transient service using runtime type information. A new instance is created each time the service is requested.
    /// </summary>
    /// <param name="type">The type to register as both service and implementation.</param>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddTransient(Type type);

    /// <summary>
    /// Registers a transient service with separate service and implementation types using runtime type information. A new instance is created each time the service is requested.
    /// </summary>
    /// <param name="serviceType">The service type to register.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddTransient(Type serviceType, Type implementationType);

    /// <summary>
    /// Registers a transient service using an existing instance. A new instance is created each time the service is requested.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="implementation">The instance to register.</param>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddTransient<TImplementation>(TImplementation implementation)
        where TImplementation : class;

    /// <summary>
    /// Registers a transient service using a factory function. A new instance is created each time the service is requested.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="func">The factory function that creates the service instance.</param>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddTransient<TImplementation>(Func<IServiceResolver, TImplementation> func)
        where TImplementation: class;

    /// <summary>
    /// Registers a singleton service. A single instance is created and shared for the lifetime of the container.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type to register.</typeparam>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddSingleton<TImplementation>()
        where TImplementation : class;

    /// <summary>
    /// Registers a singleton service with separate service and implementation types. A single instance is created and shared for the lifetime of the container.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService;

    /// <summary>
    /// Registers a singleton service using runtime type information. A single instance is created and shared for the lifetime of the container.
    /// </summary>
    /// <param name="type">The type to register as both service and implementation.</param>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddSingleton(Type type);

    /// <summary>
    /// Registers a singleton service with separate service and implementation types using runtime type information. A single instance is created and shared for the lifetime of the container.
    /// </summary>
    /// <param name="serviceType">The service type to register.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddSingleton(Type serviceType, Type implementationType);

    /// <summary>
    /// Registers a singleton service using an existing instance. The instance is shared for the lifetime of the container.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="implementation">The instance to register.</param>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddSingleton<TImplementation>(TImplementation implementation)
        where TImplementation : class;

    /// <summary>
    /// Registers a singleton service using a factory function. A single instance is created and shared for the lifetime of the container.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="func">The factory function that creates the service instance.</param>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddSingleton<TImplementation>(Func<IServiceResolver, TImplementation> func)
        where TImplementation : class;

    /// <summary>
    /// Creates a factory for creating service resolver scopes from the registered services.
    /// </summary>
    /// <returns>A service resolver factory.</returns>
    IServiceResolverFactory CreateServiceResolverFactory();

    /// <summary>
    /// Registers the service resolver as a resolvable service within the container.
    /// </summary>
    /// <returns>The service container for method chaining.</returns>
    IBenzeneServiceContainer AddServiceResolver();
}