using System.IO;
using System.Text;
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
    public async Task ExecuteAsync_SuccessfulResponse_ReturnsHealthy()
    {
        var mockLambdaClient = new Mock<IAmazonLambda>();
        mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), default))
            .ReturnsAsync(new InvokeResponse
            {
                Payload = ToPayloadStream("{\"status\":\"Ok\"}")
            });

        var healthCheck = new AwsLambdaHealthCheck("some-lambda", mockLambdaClient.Object, NullLogger<AwsLambdaHealthCheck>.Instance);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal("Lambda", healthCheck.Type);
        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        mockLambdaClient.Verify(x => x.InvokeAsync(
            It.Is<InvokeRequest>(r => r.InvocationType == InvocationType.Event), default));
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("Lambda", dependency.Kind);
        Assert.Equal("some-lambda", dependency.Name);
    }
}
