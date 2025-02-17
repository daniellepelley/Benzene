namespace Benzene.Abstractions.DI;

public interface IRegisterDependency
{
    void Register(Action<IBenzeneServiceContainer> action);
}