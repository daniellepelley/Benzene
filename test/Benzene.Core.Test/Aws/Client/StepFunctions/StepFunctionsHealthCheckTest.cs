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
    public async Task Reachability_OkResponse_ReturnsHealthy_NonDestructively()
    {
        var mock = new Mock<IAmazonStepFunctions>();
        mock.Setup(x => x.DescribeStateMachineAsync(It.IsAny<DescribeStateMachineRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DescribeStateMachineResponse { HttpStatusCode = HttpStatusCode.OK });

        var healthCheck = new StepFunctionsHealthCheck("some-state-machine-arn", mock.Object);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("StepFunctions", healthCheck.Type);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("StateMachine", dependency.Kind);
        Assert.Equal("some-state-machine-arn", dependency.Name);
        // The default probe is read-only — it must NOT start an execution.
        mock.Verify(x => x.StartExecutionAsync(It.IsAny<StartExecutionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reachability_NonOkResponse_ReturnsUnhealthy()
    {
        var mock = new Mock<IAmazonStepFunctions>();
        mock.Setup(x => x.DescribeStateMachineAsync(It.IsAny<DescribeStateMachineRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DescribeStateMachineResponse { HttpStatusCode = HttpStatusCode.InternalServerError });

        var result = await new StepFunctionsHealthCheck("some-state-machine-arn", mock.Object).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
    }

    [Fact]
    public async Task Active_StartsAnExecution_AndReportsUnderTheActiveType()
    {
        var mock = new Mock<IAmazonStepFunctions>();
        mock.Setup(x => x.StartExecutionAsync(It.IsAny<StartExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartExecutionResponse { HttpStatusCode = HttpStatusCode.OK });

        var healthCheck = new StepFunctionsHealthCheck("some-state-machine-arn", mock.Object, HealthCheckMode.Active);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("StepFunctions.Active", healthCheck.Type);
        mock.Verify(x => x.StartExecutionAsync(It.IsAny<StartExecutionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
