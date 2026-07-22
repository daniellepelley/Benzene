using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Benzene.Clients;
using Benzene.Clients.Aws.Lambda;
using Benzene.Mesh.Aws.Lambda;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Dispatch;
using Moq;
using Xunit;

namespace Benzene.Mesh.Test;

public class MeshDispatchGateTest
{
    private sealed class StubEnvironment : IMeshDispatchEnvironment
    {
        public StubEnvironment(bool isProduction) => IsProduction = isProduction;
        public bool IsProduction { get; }
    }

    [Theory]
    [InlineData(false, false, true)]  // non-prod, no override -> allowed
    [InlineData(false, true, true)]   // non-prod, override -> allowed
    [InlineData(true, false, false)]  // prod, no override -> BLOCKED (the safe default)
    [InlineData(true, true, true)]    // prod, override -> allowed
    public void IsAllowed_RespectsEnvironmentAndOption(bool isProduction, bool allowInProduction, bool expected)
    {
        var gate = new MeshDispatchGate(
            new MeshDispatchOptions { AllowInProduction = allowInProduction },
            new StubEnvironment(isProduction));

        Assert.Equal(expected, gate.IsAllowed);
    }
}

public class MeshDispatchMessageHandlerTest
{
    private sealed class StubEnvironment : IMeshDispatchEnvironment
    {
        public StubEnvironment(bool isProduction) => IsProduction = isProduction;
        public bool IsProduction { get; }
    }

    private sealed class RecordingDispatcher : IMeshServiceDispatcher
    {
        private readonly MeshDispatchResult _result;
        public RecordingDispatcher(string key, MeshDispatchResult result) { Key = key; _result = result; }
        public string Key { get; }
        public MeshServiceRegistryEntry? Entry { get; private set; }
        public MeshDispatchEnvelope? Envelope { get; private set; }

        public Task<MeshDispatchResult> DispatchAsync(MeshServiceRegistryEntry entry, MeshDispatchEnvelope envelope, CancellationToken cancellationToken)
        {
            Entry = entry;
            Envelope = envelope;
            return Task.FromResult(_result);
        }
    }

    private static MeshDispatchMessageHandler Handler(bool isProduction, MeshServiceRegistry registry, params IMeshServiceDispatcher[] dispatchers)
    {
        var gate = new MeshDispatchGate(new MeshDispatchOptions(), new StubEnvironment(isProduction));
        return new MeshDispatchMessageHandler(gate, registry, dispatchers);
    }

    private static MeshServiceRegistry HttpRegistry() =>
        new(new[] { new MeshServiceRegistryEntry("orders", "https://orders.example/spec", "https://orders.example/health") });

    [Fact]
    public async Task BlockedInProduction_ReturnsForbidden_AndNeverDispatches()
    {
        var dispatcher = new RecordingDispatcher(MeshServiceSource.Http, new MeshDispatchResult("Ok", "{}"));
        var handler = Handler(isProduction: true, HttpRegistry(), dispatcher);

        var result = await handler.HandleAsync(new MeshDispatchRequest { Service = "orders", Topic = "order:create", Body = "{}" });

        Assert.Equal("Forbidden", result.Status);
        Assert.False(result.IsSuccessful);
        Assert.Null(dispatcher.Entry); // the real handler was never invoked
    }

    [Fact]
    public async Task UnknownService_ReturnsNotFound()
    {
        var handler = Handler(false, new MeshServiceRegistry(Array.Empty<MeshServiceRegistryEntry>()),
            new RecordingDispatcher(MeshServiceSource.Http, new MeshDispatchResult("Ok", "{}")));

        var result = await handler.HandleAsync(new MeshDispatchRequest { Service = "ghost", Topic = "x" });

        Assert.Equal("NotFound", result.Status);
    }

    [Fact]
    public async Task MissingTopic_ReturnsBadRequest()
    {
        var handler = Handler(false, HttpRegistry());

        var result = await handler.HandleAsync(new MeshDispatchRequest { Service = "orders" });

        Assert.Equal("BadRequest", result.Status);
    }

    [Fact]
    public async Task NoDispatcherForSource_ReturnsNotImplemented()
    {
        var registry = new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders", "", "", MeshServiceSource.AwsLambdaInvoke,
                new Dictionary<string, string> { ["functionName"] = "fn" }),
        });
        // Only an HTTP dispatcher is registered - nothing handles AwsLambdaInvoke.
        var handler = Handler(false, registry, new RecordingDispatcher(MeshServiceSource.Http, new MeshDispatchResult("Ok", "{}")));

        var result = await handler.HandleAsync(new MeshDispatchRequest { Service = "orders", Topic = "x" });

        Assert.Equal("NotImplemented", result.Status);
    }

    [Fact]
    public async Task HappyPath_DispatchesViaMatchingTransport_AndReturnsTheServiceResponse()
    {
        var dispatcher = new RecordingDispatcher(MeshServiceSource.Http, new MeshDispatchResult("Created", "{\"id\":1}"));
        var handler = Handler(false, HttpRegistry(), dispatcher);

        var result = await handler.HandleAsync(new MeshDispatchRequest
        {
            Service = "orders",
            Topic = "order:create",
            Headers = new Dictionary<string, string> { ["k"] = "v" },
            Body = "{\"a\":1}",
        });

        Assert.Equal("Ok", result.Status);
        Assert.True(result.IsSuccessful);
        Assert.Equal("orders", dispatcher.Entry!.Name);
        Assert.Equal("order:create", dispatcher.Envelope!.Topic);
        Assert.Equal("{\"a\":1}", dispatcher.Envelope!.Body);
        // The service's response envelope is serialized into the payload.
        Assert.Contains("Created", result.Payload.Content);
        Assert.Contains("id", result.Payload.Content);
    }
}

public class AwsLambdaMeshServiceDispatcherTest
{
    [Fact]
    public async Task Dispatch_InvokesFunction_WithTopicAndBody_AndMapsResponse()
    {
        var client = new Mock<IAwsLambdaClient>();
        client.Setup(x => x.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(
                It.IsAny<BenzeneMessageClientRequest>(), "orders-fn", InvocationType.RequestResponse))
            .ReturnsAsync(new BenzeneMessageClientResponse("Created", "{\"ok\":true}", new Dictionary<string, string>()));

        var dispatcher = new AwsLambdaMeshServiceDispatcher(client.Object);
        var entry = new MeshServiceRegistryEntry("orders", "", "", MeshServiceSource.AwsLambdaInvoke,
            new Dictionary<string, string> { ["functionName"] = "orders-fn" });

        var result = await dispatcher.DispatchAsync(entry,
            new MeshDispatchEnvelope("order:create", new Dictionary<string, string>(), "{\"a\":1}"), CancellationToken.None);

        Assert.Equal("Created", result.StatusCode);
        Assert.Equal("{\"ok\":true}", result.Body);
        client.Verify(x => x.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(
            It.Is<BenzeneMessageClientRequest>(r => r.Topic == "order:create" && r.Body == "{\"a\":1}"),
            "orders-fn", InvocationType.RequestResponse), Times.Once);
    }

    [Fact]
    public async Task Dispatch_MissingFunctionName_Throws()
    {
        var dispatcher = new AwsLambdaMeshServiceDispatcher(new Mock<IAwsLambdaClient>().Object);
        var entry = new MeshServiceRegistryEntry("orders", "", "", MeshServiceSource.AwsLambdaInvoke, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(entry, new MeshDispatchEnvelope("t", new Dictionary<string, string>(), ""), CancellationToken.None));
    }
}
