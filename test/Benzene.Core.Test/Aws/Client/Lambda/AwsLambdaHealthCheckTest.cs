using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Benzene.Clients.Aws.Lambda;
using Benzene.HealthChecks.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Client.Lambda;

public class AwsLambdaHealthCheckTest
{
    private static MemoryStream ToPayloadStream(string json)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }

    [Fact]
    public async Task Reachability_OkResponse_ReturnsHealthy_NonDestructively()
    {
        var mockLambdaClient = new Mock<IAmazonLambda>();
        mockLambdaClient
            .Setup(x => x.GetFunctionConfigurationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetFunctionConfigurationResponse { HttpStatusCode = HttpStatusCode.OK });

        var healthCheck = new AwsLambdaHealthCheck("some-lambda", mockLambdaClient.Object, NullLogger<AwsLambdaHealthCheck>.Instance);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal("Lambda", healthCheck.Type);
        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("Lambda", dependency.Kind);
        Assert.Equal("some-lambda", dependency.Name);
        // The default probe is read-only — it must NOT invoke the function.
        mockLambdaClient.Verify(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task Reachability_NonOkResponse_ReturnsUnhealthy()
    {
        var mockLambdaClient = new Mock<IAmazonLambda>();
        mockLambdaClient
            .Setup(x => x.GetFunctionConfigurationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetFunctionConfigurationResponse { HttpStatusCode = HttpStatusCode.NotFound });

        var result = await new AwsLambdaHealthCheck("some-lambda", mockLambdaClient.Object, NullLogger<AwsLambdaHealthCheck>.Instance).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
    }

    [Fact]
    public async Task Active_InvokesTheFunction_AndReportsUnderTheActiveType()
    {
        var mockLambdaClient = new Mock<IAmazonLambda>();
        mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), default))
            .ReturnsAsync(new InvokeResponse { Payload = ToPayloadStream("{\"status\":\"Ok\"}") });

        var healthCheck = new AwsLambdaHealthCheck("some-lambda", mockLambdaClient.Object, NullLogger<AwsLambdaHealthCheck>.Instance, HealthCheckMode.Active);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal("Lambda.Active", healthCheck.Type);
        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        mockLambdaClient.Verify(x => x.InvokeAsync(
            It.Is<InvokeRequest>(r => r.InvocationType == InvocationType.Event), default), Times.Once);
    }
}
