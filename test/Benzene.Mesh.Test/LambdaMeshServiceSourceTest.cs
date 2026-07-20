using Amazon.Lambda;
using Benzene.Clients;
using Benzene.Clients.Aws.Lambda;
using Benzene.Mesh.Aws.Lambda;
using Benzene.Mesh.Contracts;
using Moq;
using Xunit;
using HealthChecksConstants = Benzene.HealthChecks.Constants;
using SchemaOpenApiConstants = Benzene.Schema.OpenApi.Constants;

namespace Benzene.Mesh.Test;

public class LambdaMeshServiceSourceTest
{
    private static MeshServiceRegistryEntry Entry(string? functionName = "orders-fn") =>
        new("orders-fn", "n/a", "n/a", MeshServiceSource.AwsLambdaInvoke,
            functionName == null ? null : new Dictionary<string, string> { { LambdaMeshServiceSource.FunctionNameOption, functionName } });

    [Fact]
    public async Task FetchSpecAsync_InvokesTargetFunction_ReturnsResponseBody()
    {
        var client = new Mock<IAwsLambdaClient>();
        client
            .Setup(x => x.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(
                It.IsAny<BenzeneMessageClientRequest>(), "orders-fn", InvocationType.RequestResponse))
            .ReturnsAsync(new BenzeneMessageClientResponse("Ok", "{\"info\":{\"title\":\"orders-api\"}}"));
        var source = new LambdaMeshServiceSource(client.Object);

        var result = await source.FetchSpecAsync(Entry(), CancellationToken.None);

        Assert.Equal("{\"info\":{\"title\":\"orders-api\"}}", result);
    }

    [Fact]
    public async Task TryFetchSpecAsync_InvokesSpecTopicWithTheRequestedType_ReturnsBody()
    {
        // The composite-AsyncAPI feature: the aggregator asks each Lambda-invoke source for the
        // asyncapi spec, which invokes the same "spec" topic but with a SpecRequest body selecting
        // the type (the empty-body FetchSpecAsync invoke yields the default benzene spec).
        BenzeneMessageClientRequest? sent = null;
        var client = new Mock<IAwsLambdaClient>();
        client
            .Setup(x => x.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(
                It.IsAny<BenzeneMessageClientRequest>(), "orders-fn", InvocationType.RequestResponse))
            .Callback<BenzeneMessageClientRequest, string, InvocationType>((request, _, _) => sent = request)
            .ReturnsAsync(new BenzeneMessageClientResponse("Ok", "{\"asyncapi\":\"2.0.0\"}"));
        var source = new LambdaMeshServiceSource(client.Object);

        var result = await source.TryFetchSpecAsync(Entry(), "asyncapi", CancellationToken.None);

        Assert.Equal("{\"asyncapi\":\"2.0.0\"}", result);
        Assert.Equal("spec", sent!.Topic);
        Assert.Contains("asyncapi", sent.Body); // the SpecRequest body carries type=asyncapi
    }

    [Fact]
    public async Task FetchSpecAsync_SendsTheSharedSpecTopic_MatchingBenzeneSchemaOpenApi()
    {
        // Cross-check: LambdaMeshServiceSource hardcodes its topic literal (deliberately, to avoid
        // pulling Benzene.Schema.OpenApi into this package - see its CLAUDE.md) rather than
        // referencing Constants.DefaultSpecTopic directly. This proves the literal still matches
        // the real constant, so a future rename of either fails loudly here instead of silently
        // breaking spec collection for every AWS-Lambda-Invoke-sourced service.
        string? sentTopic = null;
        var client = new Mock<IAwsLambdaClient>();
        client
            .Setup(x => x.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(
                It.IsAny<BenzeneMessageClientRequest>(), "orders-fn", InvocationType.RequestResponse))
            .Callback<BenzeneMessageClientRequest, string, InvocationType>((request, _, _) => sentTopic = request.Topic)
            .ReturnsAsync(new BenzeneMessageClientResponse("Ok", "{}"));
        var source = new LambdaMeshServiceSource(client.Object);

        await source.FetchSpecAsync(Entry(), CancellationToken.None);

        Assert.Equal(SchemaOpenApiConstants.DefaultSpecTopic, sentTopic);
    }

    [Fact]
    public async Task FetchHealthAsync_SendsTheSharedHealthCheckTopic_MatchingBenzeneHealthChecks()
    {
        string? sentTopic = null;
        var client = new Mock<IAwsLambdaClient>();
        client
            .Setup(x => x.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(
                It.IsAny<BenzeneMessageClientRequest>(), "orders-fn", InvocationType.RequestResponse))
            .Callback<BenzeneMessageClientRequest, string, InvocationType>((request, _, _) => sentTopic = request.Topic)
            .ReturnsAsync(new BenzeneMessageClientResponse("Ok", "{}"));
        var source = new LambdaMeshServiceSource(client.Object);

        await source.FetchHealthAsync(Entry(), CancellationToken.None);

        Assert.Equal(HealthChecksConstants.DefaultHealthCheckTopic, sentTopic);
    }

    [Fact]
    public async Task FetchSpecAsync_NoFunctionNameInSourceOptions_ThrowsInvalidOperationException()
    {
        var client = new Mock<IAwsLambdaClient>();
        var source = new LambdaMeshServiceSource(client.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => source.FetchSpecAsync(Entry(functionName: null), CancellationToken.None));
    }

    [Fact]
    public async Task FetchSpecAsync_CancelledToken_ThrowsEvenThoughUnderlyingClientHasNoCancellationSupport()
    {
        // IAwsLambdaClient.SendMessageAsync has no CancellationToken parameter - this proves the
        // WaitAsync(cancellationToken) wrapper still makes MeshAggregator's PerServiceFetchTimeout
        // bound this source's fetch time from the caller's point of view.
        var never = new TaskCompletionSource<BenzeneMessageClientResponse>();
        var client = new Mock<IAwsLambdaClient>();
        client
            .Setup(x => x.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(
                It.IsAny<BenzeneMessageClientRequest>(), "orders-fn", InvocationType.RequestResponse))
            .Returns(never.Task);
        var source = new LambdaMeshServiceSource(client.Object);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.FetchSpecAsync(Entry(), cts.Token));
    }
}
