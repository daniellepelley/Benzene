using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Benzene.Clients;
using Benzene.Clients.Aws.Lambda;
using Benzene.Results;
using Benzene.Test.Clients.Aws.Samples;
using Benzene.Test.Examples;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients.Aws.Lambda;

public class AwsLambdaBenzeneMessageClientTest
{
    [Fact]
    public async Task RequestAndResponse()
    {
        var mockInnerAwsLambdaClient = new Mock<IAmazonLambda>();

        var client = new AwsLambdaBenzeneMessageClient(Defaults.LambdaName, mockInnerAwsLambdaClient.Object, NullLogger.Instance);
        var result = await client.SendMessageAsync<ExamplePayload, ExamplePayload>("some-topic", new ExamplePayload());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task FireAndForget()
    {
        var mockInnerAwsLambdaClient = new Mock<IAmazonLambda>();

        var client = new AwsLambdaBenzeneMessageClient(Defaults.LambdaName, mockInnerAwsLambdaClient.Object, NullLogger.Instance);
        var result = await client.SendMessageAsync<ExamplePayload, Void >("some-topic", new ExamplePayload());

        Assert.NotNull(result);
    }

    // A user type that is coincidentally named "Void" but is NOT Benzene.Abstractions.Results.Void.
    private static class Other
    {
        public class Void { }
    }

    [Fact]
    public async Task ResponseTypeNamedVoidButNotBenzeneVoid_UsesRequestResponse()
    {
        // Invocation type must be chosen by type identity, not by the type's simple name. A user type
        // merely named "Void" must still be a request/response invocation, not silently fire-and-forget
        // (which would drop the response).
        InvokeRequest captured = null;
        var mockInnerAwsLambdaClient = new Mock<IAmazonLambda>();
        mockInnerAwsLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InvokeRequest, CancellationToken>((request, _) => captured = request);

        var client = new AwsLambdaBenzeneMessageClient(Defaults.LambdaName, mockInnerAwsLambdaClient.Object, NullLogger.Instance);
        await client.SendMessageAsync<ExamplePayload, Other.Void>("some-topic", new ExamplePayload());

        Assert.NotNull(captured);
        Assert.Equal(InvocationType.RequestResponse, captured.InvocationType);
    }

    [Fact]
    public async Task Failure()
    {
        var mockInnerAwsLambdaClient = new Mock<IAmazonLambda>();
        mockInnerAwsLambdaClient.Setup(x =>
                x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception());

        var client = new AwsLambdaBenzeneMessageClient(Defaults.LambdaName, mockInnerAwsLambdaClient.Object, NullLogger.Instance);
        var result = await client.SendMessageAsync<ExamplePayload, ExamplePayload>("some-topic", new ExamplePayload());

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }
}
