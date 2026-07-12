using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Benzene.Abstractions.Logging;
using Benzene.Clients.Aws.StepFunctions;
using Benzene.Results;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Client.StepFunctions;

public class StepFunctionsClientTest
{
    [Fact]
    public async Task StartExecutionAsync_Success_ReturnsAccepted()
    {
        var mockStepFunctionsClient = new Mock<IAmazonStepFunctions>();
        mockStepFunctionsClient
            .Setup(x => x.StartExecutionAsync(It.IsAny<StartExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartExecutionResponse());

        var client = new StepFunctionsClient("some-state-machine-arn", mockStepFunctionsClient.Object, Mock.Of<IBenzeneLogger>());

        var result = await client.StartExecutionAsync<string, string>("some-message");

        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }

    [Fact]
    public async Task StartExecutionAsync_ThrowingClient_ReturnsServiceUnavailable()
    {
        var mockStepFunctionsClient = new Mock<IAmazonStepFunctions>();
        mockStepFunctionsClient
            .Setup(x => x.StartExecutionAsync(It.IsAny<StartExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var client = new StepFunctionsClient("some-state-machine-arn", mockStepFunctionsClient.Object, Mock.Of<IBenzeneLogger>());

        var result = await client.StartExecutionAsync<string, string>("some-message");

        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var client = new StepFunctionsClient("some-state-machine-arn", Mock.Of<IAmazonStepFunctions>(), Mock.Of<IBenzeneLogger>());

        client.Dispose();
    }

    [Fact]
    public void Create_ReturnsStepFunctionsClient()
    {
        var factory = new StepFunctionsClientFactory("some-state-machine-arn", Mock.Of<IAmazonStepFunctions>(), Mock.Of<IBenzeneLogger>());

        using var client = factory.Create();

        Assert.IsType<StepFunctionsClient>(client);
    }
}
