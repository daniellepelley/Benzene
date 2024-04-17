using Benzene.Abstractions.DI;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Core;

public interface IAzureAppBuilder : IRegisterDependency
{
    void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication> func);
    IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
    IAzureApp Create(IServiceResolverFactory serviceResolverFactory);
}
