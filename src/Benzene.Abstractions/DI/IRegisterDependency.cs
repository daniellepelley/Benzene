namespace Benzene.Abstractions.DI;

/// <summary>
/// Represents a module or component that can register its dependencies with the DI container.
/// This interface enables modular dependency registration where each module is responsible for its own dependencies.
/// </summary>
public interface IRegisterDependency
{
    /// <summary>
    /// Registers the module's dependencies with the service container.
    /// </summary>
    /// <param name="action">An action that receives the service container for dependency registration.</param>
    void Register(Action<IBenzeneServiceContainer> action);
}