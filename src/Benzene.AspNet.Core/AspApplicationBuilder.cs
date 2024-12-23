using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

public class AspApplicationBuilder : IAspApplicationBuilder
{
    private readonly List<Func<IServiceResolverFactory, IEntryPointMiddlewareApplication>> _apps = new();
    private IApplicationBuilder _applicationBuilder;
    private readonly IBenzeneServiceContainer _benzeneServiceContainer;

    public AspApplicationBuilder(IApplicationBuilder applicationBuilder)
    {
        _applicationBuilder = applicationBuilder;
        
        var microsoftBenzeneServiceContainer =
            new MicrosoftServiceResolverFactory(applicationBuilder.ApplicationServices)
                    .CreateScope()
                    .GetService<IBenzeneServiceContainer>() as
                MicrosoftBenzeneServiceContainer;

        microsoftBenzeneServiceContainer.Reopen();
        _benzeneServiceContainer = microsoftBenzeneServiceContainer;
    }
    public void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication<HttpContext>> func)
    {

        var serviceResolverFactory = _benzeneServiceContainer.CreateServiceResolverFactory();
        var entryPoint = func(serviceResolverFactory);
        
        _applicationBuilder.Use(async (context, next) =>
        {
            await entryPoint.SendAsync(context);
            if (!context.Response.HasStarted)
            {
                await next();
            }
        });

    }
    
    public void Register(Action<IBenzeneServiceContainer> action)
    {
        action(_benzeneServiceContainer);
    }
    
    public IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>()
    {
        return new MiddlewarePipelineBuilder<TNewContext>(_benzeneServiceContainer);
    }
}