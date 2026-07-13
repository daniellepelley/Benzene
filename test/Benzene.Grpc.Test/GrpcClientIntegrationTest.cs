using Benzene.Clients;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Grpc.AspNet;
using Benzene.Grpc.Client;
using Benzene.Grpc.Serialization;
using Benzene.Grpc.Test.Handlers;
using Benzene.Grpc.Test.Protos;
using Benzene.Microsoft.Dependencies;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Benzene.Grpc.Test;

/// <summary>
/// End-to-end coverage for <see cref="GrpcBenzeneMessageClient"/> against a real in-process host (see
/// <see cref="GrpcHostingTest"/> for the server-side equivalent): the request is sent as the protobuf wire
/// type directly, but the response is requested as a POCO, exercising the real protobuf-to-POCO adapter
/// conversion over an actual gRPC round trip rather than a hand-rolled <see cref="Helpers.TestCallInvoker"/>.
/// </summary>
public class GrpcClientIntegrationTest
{
    [Fact]
    public async Task SendMessageAsync_RoundTripsAProtobufRequestAndAPocoResponseOverARealCall()
    {
        using var host = await BuildHostAsync();
        var channel = CreateChannel(host);
        var registry = new GrpcClientRouteRegistry();
        registry.Add<EchoRequest, EchoReply>("echo-topic", "/benzene.test.TestService/Echo");
        var adapter = new ProtobufJsonGrpcMessageAdapter();

        var client = new GrpcBenzeneMessageClient(channel, registry, adapter, new DefaultGrpcStatusReverseMapper(),
            NullLogger<GrpcBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<EchoRequest, EchoReplyPoco>(
            new BenzeneClientRequest<EchoRequest>("echo-topic", new EchoRequest { Name = "world" }, new Dictionary<string, string>()));

        Assert.True(result.IsSuccessful);
        Assert.Equal("Hello world", result.Payload.Message);
    }

    private static async Task<IHost> BuildHostAsync()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddBenzeneGrpc();
                    services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage().AddMessageHandlers(new[] { typeof(EchoMessageHandler) }).AddGrpcMessageHandlers());
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapGrpcService<TestGrpcService>());
                    app.UseBenzene(x => x.UseGrpc(grpc => grpc.UseMessageHandlers(typeof(EchoMessageHandler))));
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
}
