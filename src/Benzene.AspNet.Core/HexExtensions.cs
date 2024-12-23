using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

public static class HexExtensions
{
    public static IApplicationBuilder UseBenzene(this IApplicationBuilder app)
    {
        return app;//.UseBenzene(x => x
                //.UseProcessAspNetResponse()
                // .UseProcessResponseIfHandled()
            // .UseMessageHandlers()
        //);
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

public interface IAspApplicationBuilder: IRegisterDependency
{
    void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication<HttpContext>> func);
    IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
}

