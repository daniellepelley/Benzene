namespace Benzene.Abstractions.DI;

/// <summary>
/// Provides an adapter for integrating third-party DI containers with Benzene.
/// This interface allows Benzene to work with any DI container (e.g., Microsoft.Extensions.DependencyInjection, Autofac, StructureMap)
/// by wrapping it in the IBenzeneServiceContainer abstraction.
/// </summary>
/// <typeparam name="TContainer">The type of the underlying DI container.</typeparam>
public interface IDependencyInjectionAdapter<TContainer>
{
    /// <summary>
    /// Creates a new instance of the underlying DI container.
    /// </summary>
    /// <returns>A new container instance.</returns>
    TContainer CreateContainer();

    /// <summary>
    /// Creates a Benzene service container wrapper around an existing container instance.
    /// </summary>
    /// <param name="container">The underlying DI container to wrap.</param>
    /// <returns>A Benzene service container that delegates to the underlying container.</returns>
    IBenzeneServiceContainer CreateBenzeneServiceContainer(TContainer container);

    /// <summary>
    /// Creates a Benzene service resolver factory from an existing container instance.
    /// </summary>
    /// <param name="container">The underlying DI container.</param>
    /// <returns>A service resolver factory that can create scoped resolvers.</returns>
    IServiceResolverFactory CreateBenzeneServiceResolverFactory(TContainer container);
}