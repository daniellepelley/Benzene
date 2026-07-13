using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Grpc.AspNet;
using Benzene.Grpc.Test.Handlers;
using Benzene.Grpc.Test.Protos;
using Benzene.Microsoft.Dependencies;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Benzene.Grpc.Test;

public class GrpcHostingTest
{
    [Fact]
    public async Task Echo_WhenRouteMatchesABenzeneHandler_ReturnsTheHandlerResponse()
    {
        using var host = await BuildHostAsync(includeEchoHandler: true);
        var client = new TestService.TestServiceClient(CreateChannel(host));

        var reply = await client.EchoAsync(new EchoRequest { Name = "world" });

        Assert.Equal("Hello world", reply.Message);
    }

    [Fact]
    public async Task Echo_WhenNoRouteMatches_FallsThroughToTheNativeService()
    {
        using var host = await BuildHostAsync(includeEchoHandler: false);
        var client = new TestService.TestServiceClient(CreateChannel(host));

        var reply = await client.EchoAsync(new EchoRequest { Name = "world" });

        Assert.Equal("Native:world", reply.Message);
    }

    [Fact]
    public void UseGrpc_NoOpOnNonAspNetPlatforms()
    {
        var app = new FakeApplicationBuilder();

        var result = app.UseGrpc(_ => { });

        Assert.Same(app, result);
        Assert.Equal("Fake", result.Platform);
    }

    private static async Task<IHost> BuildHostAsync(bool includeEchoHandler)
    {
        // IGrpcRouteFinder is resolved by ASP.NET Core's real per-request DI when it activates
        // BenzeneInterceptor, so the handler types it discovers via IMessageHandlersFinder must be
        // registered eagerly here (ConfigureServices), not only later via UseGrpc's pipeline
        // configuration (which registers against Benzene's own, separate pipeline-building container).
        var handlerTypes = includeEchoHandler ? new[] { typeof(EchoMessageHandler) } : Array.Empty<Type>();

        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddBenzeneGrpc();
                    services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage().AddMessageHandlers(handlerTypes).AddGrpcMessageHandlers());
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapGrpcService<TestGrpcService>());
                    app.UseBenzene(x => x.UseGrpc(grpc => grpc.UseMessageHandlers(handlerTypes)));
                });
            });

        return await hostBuilder.StartAsync();
    }

    private static GrpcChannel CreateChannel(IHost host)
    {
        var testServer = host.GetTestServer();
        return GrpcChannel.ForAddress(testServer.BaseAddress ?? new Uri("http://localhost"), new GrpcChannelOptions
        {
            HttpHandler = testServer.CreateHandler()
        });
    }

    private class FakeApplicationBuilder : IBenzeneApplicationBuilder
    {
        public string Platform => "Fake";

        public void Register(Action<IBenzeneServiceContainer> action)
        {
        }

        public IMiddlewarePipelineBuilder<TContext> Create<TContext>()
        {
            throw new NotSupportedException();
        }
    }
}
