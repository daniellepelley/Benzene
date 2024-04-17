using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Benzene.Abstractions.Logging;
using Benzene.Clients;
using Benzene.Clients.Aws.Lambda;
using Benzene.Elements.Core.Results;
using Benzene.Results;
using Benzene.Test.Clients.Aws.Samples;
using Benzene.Test.Examples;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Void = Benzene.Results.Void;

namespace Benzene.Test.Clients.Aws.Lambda;

public class AwsLambdaBenzeneMessageClientTest
{
    [Fact]
    public async Task RequestAndResponse()
    {
        var mockInnerAwsLambdaClient = new Mock<IAmazonLambda>();

        var client = new AwsLambdaBenzeneMessageClient(Defaults.LambdaName, mockInnerAwsLambdaClient.Object, Mock.Of<IBenzeneLogger>());
        var result = await client.SendMessageAsync<ExamplePayload, ExamplePayload>("some-topic", new ExamplePayload());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task FireAndForget()
    {
        var mockInnerAwsLambdaClient = new Mock<IAmazonLambda>();

        var client = new AwsLambdaBenzeneMessageClient(Defaults.LambdaName, mockInnerAwsLambdaClient.Object, Mock.Of<IBenzeneLogger>());
        var result = await client.SendMessageAsync<ExamplePayload, Void >("some-topic", new ExamplePayload());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Failure()
    {
        var mockInnerAwsLambdaClient = new Mock<IAmazonLambda>();
        mockInnerAwsLambdaClient.Setup(x =>
                x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception());

        var client = new AwsLambdaBenzeneMessageClient(Defaults.LambdaName, mockInnerAwsLambdaClient.Object, Mock.Of<IBenzeneLogger>());
        var result = await client.SendMessageAsync<ExamplePayload, ExamplePayload>("some-topic", new ExamplePayload());

        Assert.NotNull(result);
        Assert.Equal(ClientResultStatus.ServiceUnavailable, result.Status);
    }
}
