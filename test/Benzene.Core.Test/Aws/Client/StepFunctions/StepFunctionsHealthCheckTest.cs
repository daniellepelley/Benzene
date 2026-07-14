using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Benzene.Clients.Aws.StepFunctions;
using Benzene.HealthChecks.Core;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Client.StepFunctions;

public class StepFunctionsHealthCheckTest
{
    [Fact]
    public async Task ExecuteAsync_OkResponse_ReturnsHealthy()
    {
        var mockStepFunctionsClient = new Mock<IAmazonStepFunctions>();
        mockStepFunctionsClient
            .Setup(x => x.StartExecutionAsync(It.IsAny<StartExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartExecutionResponse { HttpStatusCode = HttpStatusCode.OK });

        var healthCheck = new StepFunctionsHealthCheck("some-state-machine-arn", mockStepFunctionsClient.Object);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("StepFunctions", healthCheck.Type);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("StateMachine", dependency.Kind);
        Assert.Equal("some-state-machine-arn", dependency.Name);
    }

    [Fact]
    public async Task ExecuteAsync_NonOkResponse_ReturnsUnhealthy()
    {
        var mockStepFunctionsClient = new Mock<IAmazonStepFunctions>();
        mockStepFunctionsClient
            .Setup(x => x.StartExecutionAsync(It.IsAny<StartExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartExecutionResponse { HttpStatusCode = HttpStatusCode.InternalServerError });

        var healthCheck = new StepFunctionsHealthCheck("some-state-machine-arn", mockStepFunctionsClient.Object);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
    }
}
