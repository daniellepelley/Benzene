using System.ComponentModel.Design;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.AspNet.Core;

public static class HexExtensions
{
    public static IApplicationBuilder UseBenzene(this IApplicationBuilder app)
    {
        return app.UseBenzene(x => x
                //.UseProcessAspNetResponse()
                .UseProcessResponse()
            // .UseMessageRouter()
        );
    }

    public static IApplicationBuilder UseBenzene(this IApplicationBuilder app, Action<IMiddlewarePipelineBuilder<AspNetContext>> builder)
    {
        var tempServiceResolverFactory = new MicrosoftServiceResolverFactory(app.ApplicationServices);

        var benzeneServiceContainer =
            tempServiceResolverFactory.CreateScope().GetService<IBenzeneServiceContainer>();
        
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
}