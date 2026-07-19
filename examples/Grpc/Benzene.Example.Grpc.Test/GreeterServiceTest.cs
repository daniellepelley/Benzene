using Benzene.Example.Grpc.Services;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Client = Benzene.Example.Grpc.Client;

namespace Benzene.Example.Grpc.Test;

/// <summary>
/// Drives the gRPC example's real entry point (<c>Program.cs</c>) in-memory via
/// <see cref="WebApplicationFactory{TEntryPoint}"/> - <see cref="GreeterService"/> only points the
/// factory at the server example's assembly - and calls it over a real generated
/// <c>Greeter.GreeterClient</c> stub (from the client example project) through the in-memory HTTP/2
/// test handler. Every RPC shape asserts the <b>Benzene</b> handler answered, not the native
/// <see cref="GreeterService"/>: the interceptor routes any method matching a <c>[GrpcMethod]</c>
/// handler through Benzene's pipeline and only falls back to the native service otherwise, and the
/// unary handler's distinctive "…this is Benzene" reply is the proof it won.
/// </summary>
public class GreeterServiceTest : IClassFixture<WebApplicationFactory<GreeterService>>
{
    private readonly WebApplicationFactory<GreeterService> _factory;

    public GreeterServiceTest(WebApplicationFactory<GreeterService> factory)
    {
        _factory = factory;
    }

    private Client.Greeter.GreeterClient CreateClient()
    {
        var server = _factory.Server;
        var channel = GrpcChannel.ForAddress(server.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = server.CreateHandler()
        });
        return new Client.Greeter.GreeterClient(channel);
    }

    [Fact]
    public async Task SayHello_RoutesThroughBenzene_NotTheNativeGreeterService()
    {
        var client = CreateClient();

        var reply = await client.SayHelloAsync(new Client.HelloRequest { Name = "acme" });

        // The native GreeterService.SayHello would return just "Hello acme"; the Benzene
        // SayHelloMessageHandler (matched by [GrpcMethod("/greet.Greeter/SayHello")]) appends
        // ", this is Benzene" - so this exact string proves the interceptor routed to Benzene.
        Assert.Equal("Hello acme, this is Benzene", reply.Message);
    }

    [Fact]
    public async Task SayHelloServerStream_StreamsEverySalutationBack()
    {
        var client = CreateClient();

        using var call = client.SayHelloServerStream(new Client.HelloRequest { Name = "acme" });
        var messages = new List<string>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
        {
            messages.Add(call.ResponseStream.Current.Message);
        }

        Assert.Equal(new[] { "Hello acme", "Hi acme", "Hey acme" }, messages);
    }

    [Fact]
    public async Task SayHelloClientStream_AggregatesEveryUploadedName()
    {
        var client = CreateClient();

        using var call = client.SayHelloClientStream();
        foreach (var name in new[] { "Alice", "Bob", "Carol" })
        {
            await call.RequestStream.WriteAsync(new Client.HelloRequest { Name = name });
        }
        await call.RequestStream.CompleteAsync();

        var reply = await call.ResponseAsync;
        Assert.Equal("Hello Alice, Bob, Carol", reply.Message);
    }

    [Fact]
    public async Task SayHelloBidiStream_GreetsEachNameAsItArrives()
    {
        var client = CreateClient();

        using var call = client.SayHelloBidiStream();
        var received = new List<string>();
        var reader = Task.Run(async () =>
        {
            while (await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                received.Add(call.ResponseStream.Current.Message);
            }
        });

        foreach (var name in new[] { "Dave", "Erin" })
        {
            await call.RequestStream.WriteAsync(new Client.HelloRequest { Name = name });
        }
        await call.RequestStream.CompleteAsync();
        await reader;

        Assert.Equal(new[] { "Hello Dave", "Hello Erin" }, received);
    }
}
