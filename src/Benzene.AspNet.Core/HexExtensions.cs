using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;

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
        var serviceResolverFactory = new MicrosoftServiceResolverFactory(app.ApplicationServices);

        var middlewarePipelineBuilder = serviceResolverFactory.CreateScope().GetService<IMiddlewarePipelineBuilder<AspNetContext>>();
        
        builder(middlewarePipelineBuilder);

        var pipeline = middlewarePipelineBuilder.Build();

        app.Use(async (context, next) =>
        {
            await pipeline.HandleAsync(new AspNetContext(context),
                new MicrosoftServiceResolverAdapter(context.RequestServices));

            if (!context.Response.HasStarted)
            {
                await next();
            }
        });
        return app;
    }
}