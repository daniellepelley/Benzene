using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Core;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.AspNet.Core;

/// <summary>
/// Provides the top-level extension methods for wiring Benzene into an ASP.NET Core application.
/// </summary>
public static class BenzeneExtensions
{
    /// <summary>
    /// Adds an HTTP entry point application to the ASP.NET Core app, configuring its inner middleware
    /// pipeline. This is the ASP.NET Core-specific building block <see cref="UseBenzene(WebApplicationBuilder)"/>
    /// and <see cref="UseBenzene(IApplicationBuilder)"/> use to run a platform-neutral <see cref="BenzeneStartUp"/>;
    /// call it directly only if you're wiring ASP.NET Core up by hand instead.
    /// </summary>
    /// <param name="app">The Benzene ASP.NET application builder to add HTTP handling to.</param>
    /// <param name="action">The action that configures the HTTP middleware pipeline.</param>
    /// <returns>The Benzene ASP.NET application builder, for method chaining.</returns>
    public static IAspApplicationBuilder UseHttp(this IAspApplicationBuilder app, Action<IMiddlewarePipelineBuilder<AspNetContext>> action)
    {
        var pipeline = app.Create<AspNetContext>();
        app.Register(x => x
            .AddSingleton<IMiddlewarePipelineBuilder<AspNetContext>, MiddlewarePipelineBuilder<AspNetContext>>()
            .AddAspNetMessageHandlers());

        // Seed the scope's ambient cancellation token from the request's aborted token, so any
        // component resolving ICancellationTokenAccessor (e.g. a health check) observes a client
        // disconnect / request abort. Runs first, in the same per-request scope as the rest.
        pipeline.Use(resolver => new FuncWrapperMiddleware<AspNetContext>("SeedCancellationToken", async (context, next) =>
        {
            var accessor = resolver.TryGetService<CancellationTokenAccessor>();
            if (accessor != null)
            {
                accessor.CancellationToken = context.HttpContext.RequestAborted;
            }
            await next();
        }));

        action(pipeline);
        app.Add(serviceResolverFactory => new AspNetApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }

    /// <summary>
    /// Applies HTTP-specific configuration to a platform-neutral <see cref="IBenzeneApplicationBuilder"/>.
    /// No-op on any platform other than ASP.NET Core.
    /// </summary>
    /// <param name="app">The application builder passed to <see cref="BenzeneStartUp.Configure"/>.</param>
    /// <param name="action">The action that configures the HTTP middleware pipeline.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseHttp(this IBenzeneApplicationBuilder app, Action<IMiddlewarePipelineBuilder<AspNetContext>> action)
    {
        if (app is IAspApplicationBuilder aspApp)
        {
            aspApp.UseHttp(action);
        }
        return app;
    }

    /// <summary>
    /// Wires Benzene into the ASP.NET Core application pipeline, wrapping <paramref name="app"/> in a
    /// <see cref="Benzene.AspNet.Core.AspApplicationBuilder"/> and running the given configuration action
    /// against it.
    /// </summary>
    /// <param name="app">The ASP.NET Core application builder.</param>
    /// <param name="builder">The action that configures Benzene (typically via <see cref="UseHttp(IAspApplicationBuilder, Action{IMiddlewarePipelineBuilder{AspNetContext}})"/>).</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IApplicationBuilder UseBenzene(this IApplicationBuilder app, Action<IAspApplicationBuilder> builder)
    {
        var aspApplicationBuilder = new AspApplicationBuilder(app);
        aspApplicationBuilder.Register(x => x.AddBenzene());
        builder(aspApplicationBuilder);
        return app;
    }

    /// <summary>
    /// Registers a platform-neutral <see cref="BenzeneStartUp"/>'s services with the ASP.NET Core host,
    /// running <see cref="IStartUp{TContainer,TConfiguration,TAppBuilder}.GetConfiguration"/> and
    /// <see cref="IStartUp{TContainer,TConfiguration,TAppBuilder}.ConfigureServices"/>. Pair with
    /// <see cref="UseBenzene(IApplicationBuilder)"/> after <c>Build()</c> to run <c>Configure</c>.
    /// </summary>
    /// <typeparam name="TStartUp">The <see cref="BenzeneStartUp"/> to run.</typeparam>
    /// <param name="builder">The web application builder.</param>
    /// <returns><paramref name="builder"/>, for method chaining.</returns>
    public static WebApplicationBuilder UseBenzene<TStartUp>(this WebApplicationBuilder builder)
        where TStartUp : BenzeneStartUp, new()
    {
        var startUp = new TStartUp();
        var configuration = startUp.GetConfiguration();
        startUp.ConfigureServices(builder.Services, configuration);
        builder.Services.AddSingleton(new BenzeneStartUpHolder(startUp, configuration));
        return builder;
    }

    /// <summary>
    /// Runs the <c>Configure</c> method of the <see cref="BenzeneStartUp"/> registered by
    /// <see cref="UseBenzene{TStartUp}(WebApplicationBuilder)"/> against the built application, wiring its
    /// middleware pipeline into the ASP.NET Core request pipeline.
    /// </summary>
    /// <param name="app">The built ASP.NET Core application.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IApplicationBuilder UseBenzene(this IApplicationBuilder app)
    {
        var holder = app.ApplicationServices.GetRequiredService<BenzeneStartUpHolder>();
        var aspApplicationBuilder = new AspApplicationBuilder(app);
        aspApplicationBuilder.Register(x => x.AddBenzene());
        holder.StartUp.Configure(aspApplicationBuilder, holder.Configuration);
        return app;
    }

    private sealed class BenzeneStartUpHolder
    {
        public BenzeneStartUpHolder(BenzeneStartUp startUp, IConfiguration configuration)
        {
            StartUp = startUp;
            Configuration = configuration;
        }

        public BenzeneStartUp StartUp { get; }
        public IConfiguration Configuration { get; }
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
