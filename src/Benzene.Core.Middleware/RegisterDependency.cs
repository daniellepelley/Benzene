using Benzene.Abstractions.DI;

namespace Benzene.Core.Middleware;

public class RegisterDependency(IBenzeneServiceContainer benzeneServiceContainer) : IRegisterDependency
{
    public void Register(Action<IBenzeneServiceContainer> action)
    {
        action(benzeneServiceContainer);
    }
}