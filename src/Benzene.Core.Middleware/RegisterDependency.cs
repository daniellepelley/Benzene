using Benzene.Abstractions.DI;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides a simple implementation of <see cref="IRegisterDependency"/> that wraps a service container
/// for dependency registration.
/// </summary>
/// <remarks>
/// This class acts as an adapter between the dependency registration interface and the concrete
/// service container, allowing pipeline builders and other components to register dependencies
/// without directly coupling to the container implementation.
/// </remarks>
public class RegisterDependency(IBenzeneServiceContainer benzeneServiceContainer) : IRegisterDependency
{
    /// <summary>
    /// Registers dependencies by invoking the provided action with the service container.
    /// </summary>
    /// <param name="action">The action that performs service registration on the container.</param>
    public void Register(Action<IBenzeneServiceContainer> action)
    {
        action(benzeneServiceContainer);
    }
}