using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.MiddlewareBuilder;

public interface IRegisterDependency
{
    void Register(Action<IBenzeneServiceContainer> action);
}