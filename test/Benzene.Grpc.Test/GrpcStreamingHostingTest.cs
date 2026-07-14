using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Grpc.AspNet;
using Benzene.Grpc.Test.Handlers;
using Benzene.Grpc.Test.Protos;
using Benzene.Microsoft.Dependencies;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Benzene.Grpc.Test;

/// <summary>
/// End-to-end coverage for the three streaming <see cref="BenzeneInterceptor"/> overrides, over a real
/// in-process host and generated client (see <see cref="GrpcHostingTest"/> for the unary case). The unit-level
/// streaming behaviour of <see cref="GrpcMethodHandler"/> itself is covered by <see cref="GrpcMethodHandlerStreamingTest"/>.
/// </summary>
public class GrpcStreamingHostingTest
{
    [Fact]
    public async Task Subscribe_RoutesThroughBenzeneInterceptorAndYieldsHandlerItems()
    {
        using var host = await BuildHostAsync(typeof(SubscribeMessageHandler));
        var client = new TestService.TestServiceClient(CreateChannel(host));

        using var call = client.Subscribe(new SubscribeRequest { Topic = "t" });
        var items = new List<string>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
        {
            items.Add(call.ResponseStream.Current.Item);
        }

        Assert.Equal(new[] { "t-0", "t-1", "t-2" }, items);
    }

    [Fact]
    public async Task Upload_RoutesThroughBenzeneInterceptorAndSumsUploadedItems()
    {
        using var host = await BuildHostAsync(typeof(UploadStreamMessageHandler));
        var client = new TestService.TestServiceClient(CreateChannel(host));

        using var call = client.Upload();
        await call.RequestStream.WriteAsync(new UploadItem { Value = 1 });
        await call.RequestStream.WriteAsync(new UploadItem { Value = 2 });
        await call.RequestStream.WriteAsync(new UploadItem { Value = 3 });
        await call.RequestStream.CompleteAsync();

        var summary = await call.ResponseAsync;

        Assert.Equal(6, summary.Total);
    }

    [Fact]
    public async Task Chat_RoutesThroughBenzeneInterceptorAndEchoesEachMessage()
    {
        using var host = await BuildHostAsync(typeof(ChatMessageHandler));
        var client = new TestService.TestServiceClient(CreateChannel(host));

        using var call = client.Chat();
        var readTask = Task.Run(async () =>
        {
            var received = new List<string>();
            while (await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                received.Add(call.ResponseStream.Current.Text);
            }
            return received;
        });

        await call.RequestStream.WriteAsync(new ChatMessage { Text = "a" });
        await call.RequestStream.WriteAsync(new ChatMessage { Text = "b" });
        await call.RequestStream.CompleteAsync();

        var received = await readTask;

        Assert.Equal(new[] { "Echo: a", "Echo: b" }, received);
    }

    [Fact]
    public async Task Subscribe_WhenClientCancelsTheCall_ThrowsRpcExceptionCancelled()
    {
        using var host = await BuildHostAsync(typeof(SubscribeMessageHandler));
        var client = new TestService.TestServiceClient(CreateChannel(host));
        using var cts = new CancellationTokenSource();

        using var call = client.Subscribe(new SubscribeRequest { Topic = "t" }, cancellationToken: cts.Token);
        Assert.True(await call.ResponseStream.MoveNext(CancellationToken.None));

        cts.Cancel();

        await Assert.ThrowsAsync<RpcException>(() => call.ResponseStream.MoveNext(CancellationToken.None));
    }

    private static async Task<IHost> BuildHostAsync(Type handlerType)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddBenzeneGrpc();
                    services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage().AddMessageHandlers(new[] { handlerType }).AddGrpcMessageHandlers());
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapGrpcService<TestGrpcService>());
                    app.UseBenzene(x => x.UseGrpc(grpc => grpc.UseMessageHandlers(handlerType)));
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
