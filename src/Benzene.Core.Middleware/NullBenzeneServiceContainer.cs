using Benzene.Abstractions.DI;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides a null object implementation of <see cref="IBenzeneServiceContainer"/> for testing or scenarios
/// where a service container is not needed.
/// </summary>
/// <remarks>
/// This implementation follows the Null Object pattern, providing no-op implementations for most methods
/// and returning a <see cref="NullServiceResolverFactory"/> when creating service resolvers. All type
/// registration queries return true to prevent null reference exceptions in calling code.
/// </remarks>
public class NullBenzeneServiceContainer : IBenzeneServiceContainer
{
    private const string NotSupportedMessage =
        "NullBenzeneServiceContainer is a null-object placeholder and does not support service registration.";

    /// <summary>
    /// Determines whether a service type is registered in the container.
    /// </summary>
    /// <typeparam name="TService">The service type to check.</typeparam>
    /// <returns>Always returns true.</returns>
    public bool IsTypeRegistered<TService>()
    {
        return true;
    }

    /// <summary>
    /// Determines whether a service type is registered in the container.
    /// </summary>
    /// <param name="type">The service type to check.</param>
    /// <returns>Always returns true.</returns>
    public bool IsTypeRegistered(Type type)
    {
        return true;
    }

    /// <summary>
    /// Adds a scoped service registration.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type to register.</typeparam>
    /// <returns>This instance for method chaining.</returns>
    /// <exception cref="NotImplementedException">Always thrown as this is a null implementation.</exception>
    public IBenzeneServiceContainer AddScoped<TImplementation>() where TImplementation : class
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddScoped(Type type)
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddScoped(Type serviceType, Type implementationType)
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddScoped<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddScoped<TImplementation>(Func<IServiceResolver, TImplementation> func) where TImplementation : class
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddTransient<TImplementation>() where TImplementation : class
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddTransient(Type type)
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddTransient(Type serviceType, Type implementationType)
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddTransient<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddTransient<TImplementation>(Func<IServiceResolver, TImplementation> func) where TImplementation : class
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddSingleton<TImplementation>() where TImplementation : class
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddSingleton(Type type)
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddSingleton(Type serviceType, Type implementationType)
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddSingleton<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <inheritdoc />
    public IBenzeneServiceContainer AddSingleton<TImplementation>(Func<IServiceResolver, TImplementation> func) where TImplementation : class
    {
        throw new NotImplementedException(NotSupportedMessage);
    }

    /// <summary>
    /// Creates a service resolver factory.
    /// </summary>
    /// <returns>A null service resolver factory instance.</returns>
    public IServiceResolverFactory CreateServiceResolverFactory()
    {
        return new NullServiceResolverFactory();
    }

    /// <summary>
    /// Adds the service resolver to the container.
    /// </summary>
    /// <returns>This instance for method chaining.</returns>
    public IBenzeneServiceContainer AddServiceResolver()
    {
        return this;
    }
}