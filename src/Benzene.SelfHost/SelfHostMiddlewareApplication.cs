using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.SelfHost;

public class SelfHostMiddlewareApplication : EntryPointMiddlewareApplication<string, string>
{
    public SelfHostMiddlewareApplication(IMiddlewarePipeline<SelfHostContext> pipeline, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareApplication<string,SelfHostContext, string>(pipeline,
                request => new SelfHostContext(request),
                context => context.Response
            ),
            serviceResolverFactory)
    { }
}
