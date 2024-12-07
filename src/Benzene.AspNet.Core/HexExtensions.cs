using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandling;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

public static class HexExtensions
{
    public static IApplicationBuilder UseBenzene(this IApplicationBuilder app)
    {
        return app.UseBenzene(x => x
                //.UseProcessAspNetResponse()
                .UseProcessResponseIfHandled()
            // .UseMessageHandlers()
        );
    }

    public static IApplicationBuilder UseBenzene(this IApplicationBuilder app, Action<IMiddlewarePipelineBuilder<AspNetContext>> builder)
    {
        var tempServiceResolverFactory = new MicrosoftServiceResolverFactory(app.ApplicationServices);

        var benzeneServiceContainer =
            tempServiceResolverFactory.CreateScope().GetService<IBenzeneServiceContainer>() as
                MicrosoftBenzeneServiceContainer;
        
        benzeneServiceContainer.Reopen();
        
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<AspNetContext>(benzeneServiceContainer);
        
        builder(middlewarePipelineBuilder);

        var pipeline = middlewarePipelineBuilder.Build();
        var serviceResolverFactory = benzeneServiceContainer.CreateServiceResolverFactory();
        
        app.Use(async (context, next) =>
        {
            await pipeline.HandleAsync(new AspNetContext(context),
                serviceResolverFactory.CreateScope());

            if (!context.Response.HasStarted)
            {
                await next();
            }
        });
        return app;
    }
    
    public static IAspApplicationBuilder UseAspNet(this IAspApplicationBuilder app, Action<IMiddlewarePipelineBuilder<AspNetContext>> action)
    {
        var pipeline = app.Create<AspNetContext>();
        app.Register(x => x.AddAspNetMessageHandlers());
        action(pipeline);
        app.Add(serviceResolverFactory => new AspNetApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }
    

    public static IApplicationBuilder UseBenzene2(this IApplicationBuilder app, Action<IAspApplicationBuilder> builder)
    {
        var aspApplicationBuilder = new AspApplicationBuilder(app);
        builder(aspApplicationBuilder);
        return app;
    }

}

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
            await entryPoint.HandleAsync(context);
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

public interface IAspApplicationBuilder: IRegisterDependency
{
    void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication<HttpContext>> func);
    IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
}

