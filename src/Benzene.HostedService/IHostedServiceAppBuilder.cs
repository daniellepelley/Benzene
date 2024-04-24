using Benzene.Abstractions.DI;
using Benzene.Abstractions.MiddlewareBuilder;

namespace Benzene.HostedService;

public interface IHostedServiceAppBuilder : IRegisterDependency
{
    void Add(Func<IServiceResolverFactory, IBenzeneConsumer> func);
    IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
    IBenzeneConsumer Create(IServiceResolverFactory serviceResolverFactory);
}
