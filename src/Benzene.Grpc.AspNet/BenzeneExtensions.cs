using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.AspNet.Core;
using Benzene.Grpc;

namespace Benzene.Grpc.AspNet;

/// <summary>
/// Provides the top-level extension methods for wiring Benzene's gRPC support into an ASP.NET Core application.
/// </summary>
public static class BenzeneExtensions
{
    /// <summary>
    /// Configures the Benzene gRPC middleware pipeline and populates the <see cref="IGrpcMethodHandlerFactoryAccessor"/>
    /// that <see cref="BenzeneInterceptor"/> resolves per call. Requires <c>AddBenzeneGrpc()</c> to have been
    /// called on the service collection and gRPC services to be mapped separately (e.g. via
    /// <c>MapGrpcService&lt;TService&gt;()</c>).
    /// </summary>
    /// <param name="app">The Benzene ASP.NET application builder to add gRPC handling to.</param>
    /// <param name="action">The action that configures the gRPC middleware pipeline.</param>
    /// <returns>The Benzene ASP.NET application builder, for method chaining.</returns>
    public static IAspApplicationBuilder UseGrpc(this IAspApplicationBuilder app, Action<IMiddlewarePipelineBuilder<GrpcContext>> action)
    {
        var pipeline = app.Create<GrpcContext>();
        app.Register(x => x.AddGrpcMessageHandlers());
        action(pipeline);
        var builtPipeline = pipeline.Build();

        app.Register(x =>
        {
            using var resolver = x.CreateServiceResolverFactory().CreateScope();
            var accessor = resolver.GetService<IGrpcMethodHandlerFactoryAccessor>();
            accessor.Factory = new GrpcMethodHandlerFactory(x, builtPipeline);
        });

        return app;
    }

    /// <summary>
    /// Applies gRPC-specific configuration to a platform-neutral <see cref="IBenzeneApplicationBuilder"/>.
    /// No-op on any platform other than ASP.NET Core.
    /// </summary>
    /// <param name="app">The application builder passed to <see cref="Benzene.Microsoft.Dependencies.BenzeneStartUp.Configure"/>.</param>
    /// <param name="action">The action that configures the gRPC middleware pipeline.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseGrpc(this IBenzeneApplicationBuilder app, Action<IMiddlewarePipelineBuilder<GrpcContext>> action)
    {
        if (app is IAspApplicationBuilder aspApp)
        {
            aspApp.UseGrpc(action);
        }
        return app;
    }
}
