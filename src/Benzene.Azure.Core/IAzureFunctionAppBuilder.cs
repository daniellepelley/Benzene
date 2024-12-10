using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Azure.Core;

public interface IAzureFunctionAppBuilder : IRegisterDependency
{
    void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication> func);
    IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
    IAzureFunctionApp Create(IServiceResolverFactory serviceResolverFactory);
}
