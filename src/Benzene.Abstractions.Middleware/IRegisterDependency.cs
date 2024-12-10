using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Middleware;

public interface IRegisterDependency
{
    void Register(Action<IBenzeneServiceContainer> action);
}