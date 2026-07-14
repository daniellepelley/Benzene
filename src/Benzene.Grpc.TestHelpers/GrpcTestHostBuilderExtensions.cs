using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Microsoft.Dependencies;
using Benzene.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Benzene.Grpc.TestHelpers;

public static class GrpcTestHostBuilderExtensions
{
    /// <summary>
    /// Builds an in-memory gRPC host for <typeparamref name="TStartUp"/>, running its
    /// <c>ConfigureServices</c>/<c>Configure</c> against an ASP.NET Core <see cref="TestServer"/> (see
    /// <see cref="BenzeneTestHost.Create{TStartUp}"/> to start the builder chain).
    /// </summary>
    /// <param name="mapServices">Maps gRPC (and any other) endpoints, e.g. <c>e => e.MapGrpcService&lt;MyService&gt;()</c>.</param>
    public static GrpcTestHost BuildGrpcHost<TStartUp>(
        this BenzeneTestHostBuilder<TStartUp> builder, Action<IEndpointRouteBuilder> mapServices)
        where TStartUp : BenzeneStartUp, new()
    {
        return builder.Build((startUp, services, configuration) =>
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(collection =>
                    {
                        collection.AddRouting();
                        foreach (var descriptor in services)
                        {
                            collection.Add(descriptor);
                        }
                    });
                    webHost.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(mapServices);

                        var aspApplicationBuilder = new AspApplicationBuilder(app);
                        aspApplicationBuilder.Register(x => x.AddBenzene());
                        startUp.Configure(aspApplicationBuilder, configuration);
                    });
                });

            var host = hostBuilder.StartAsync().GetAwaiter().GetResult();
            return new GrpcTestHost(host);
        });
    }
}
