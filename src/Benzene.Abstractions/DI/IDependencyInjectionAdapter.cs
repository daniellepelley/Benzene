namespace Benzene.Abstractions.DI;

public interface IDependencyInjectionAdapter<TContainer>
{
    TContainer CreateContainer();
    IBenzeneServiceContainer CreateBenzeneServiceContainer(TContainer container);
    IServiceResolverFactory CreateBenzeneServiceResolverFactory(TContainer container);
}