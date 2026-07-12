using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

/// <summary>
/// Provides the top-level extension methods for wiring Benzene into an ASP.NET Core application.
/// </summary>
public static class BenzeneExtensions
{
    // public static IApplicationBuilder UseBenzene(this IApplicationBuilder app)
    // {
    //     return app;//.UseBenzene(x => x
    //             //.UseProcessAspNetResponse()
    //             // .UseProcessResponseIfHandled()
    //         // .UseMessageHandlers()
    //     //);
    // }
    //
    // public static IApplicationBuilder UseBenzene(this IApplicationBuilder app, Action<IMiddlewarePipelineBuilder<AspNetContext>> builder)
    // {
    //     var tempServiceResolverFactory = new MicrosoftServiceResolverFactory(app.ApplicationServices);
    //
    //     var benzeneServiceContainer =
    //         tempServiceResolverFactory.CreateScope().GetService<IBenzeneServiceContainer>() as
    //             MicrosoftBenzeneServiceContainer;
    //
    //     benzeneServiceContainer.Reopen();
    //
    //     var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<AspNetContext>(benzeneServiceContainer);
    //
    //     builder(middlewarePipelineBuilder);
    //
    //     var pipeline = middlewarePipelineBuilder.Build();
    //     var serviceResolverFactory = benzeneServiceContainer.CreateServiceResolverFactory();
    //
    //     app.Use(async (context, next) =>
    //     {
    //         await pipeline.HandleAsync(new AspNetContext(context),
    //             serviceResolverFactory.CreateScope());
    //
    //         if (!context.Response.HasStarted)
    //         {
    //             await next();
    //         }
    //     });
    //     return app;
    // }

    /// <summary>
    /// Adds an HTTP entry point application to the ASP.NET Core app, configuring its inner middleware
    /// pipeline.
    /// </summary>
    /// <param name="app">The Benzene ASP.NET application builder to add HTTP handling to.</param>
    /// <param name="action">The action that configures the HTTP middleware pipeline.</param>
    /// <returns>The Benzene ASP.NET application builder, for method chaining.</returns>
    public static IAspApplicationBuilder UseAspNet(this IAspApplicationBuilder app, Action<IMiddlewarePipelineBuilder<AspNetContext>> action)
    {
        var pipeline = app.Create<AspNetContext>();
        app.Register(x => x
            .AddSingleton<IMiddlewarePipelineBuilder<AspNetContext>, MiddlewarePipelineBuilder<AspNetContext>>()
            .AddAspNetMessageHandlers());
        action(pipeline);
        app.Add(serviceResolverFactory => new AspNetApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }

    /// <summary>
    /// Wires Benzene into the ASP.NET Core application pipeline, wrapping <paramref name="app"/> in a
    /// <see cref="Benzene.AspNet.Core.AspApplicationBuilder"/> and running the given configuration action
    /// against it.
    /// </summary>
    /// <param name="app">The ASP.NET Core application builder.</param>
    /// <param name="builder">The action that configures Benzene (typically via <see cref="UseAspNet"/>).</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IApplicationBuilder UseBenzene(this IApplicationBuilder app, Action<IAspApplicationBuilder> builder)
    {
        var aspApplicationBuilder = new AspApplicationBuilder(app);
        aspApplicationBuilder.Register(x => x.AddBenzene());
        builder(aspApplicationBuilder);
        return app;
    }

}

/// <summary>
/// Builds the set of entry point applications wired into an ASP.NET Core application's request pipeline.
/// </summary>
public interface IAspApplicationBuilder: IRegisterDependency
{
    /// <summary>
    /// Registers a factory for an entry point application, adding ASP.NET Core middleware that runs it
    /// against each incoming request.
    /// </summary>
    /// <param name="func">A factory that creates the entry point application given the current invocation's service resolver factory.</param>
    void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication<HttpContext>> func);

    /// <summary>
    /// Creates a new middleware pipeline builder for a given context type, sharing this builder's
    /// underlying service container.
    /// </summary>
    /// <typeparam name="TNewContext">The context type the pipeline operates on.</typeparam>
    /// <returns>The created pipeline builder.</returns>
    IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
}
