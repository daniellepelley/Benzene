using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Benzene.Clients.Aws.Sns;
using Benzene.HealthChecks.Core;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Client.Sns;

public class SnsHealthCheckTest
{
    [Fact]
    public async Task ExecuteAsync_OkResponse_ReturnsHealthy()
    {
        var mock = new Mock<IAmazonSimpleNotificationService>();
        mock.Setup(x => x.GetTopicAttributesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTopicAttributesResponse { HttpStatusCode = HttpStatusCode.OK });

        var result = await new SnsHealthCheck("arn:aws:sns:us-east-1:123:orders", mock.Object).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("Sns", result.Type);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("Topic", dependency.Kind);
        Assert.Equal("arn:aws:sns:us-east-1:123:orders", dependency.Name);
    }

    [Fact]
    public async Task ExecuteAsync_ClientThrows_ReturnsUnhealthy_WithTheTopicDependency()
    {
        var mock = new Mock<IAmazonSimpleNotificationService>();
        mock.Setup(x => x.GetTopicAttributesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("no such topic"));

        var result = await new SnsHealthCheck("arn:aws:sns:us-east-1:123:orders", mock.Object).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal("NotFoundException", result.Data["Error"]);
        Assert.Equal("arn:aws:sns:us-east-1:123:orders", Assert.Single(result.Dependencies).Name);
    }

    [Fact]
    public async Task ExecuteAsync_PermissionDenied_DegradesToWarning_AndSurfacesTheDiscriminators()
    {
        var mock = new Mock<IAmazonSimpleNotificationService>();
        mock.Setup(x => x.GetTopicAttributesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthorizationErrorException("not authorized")
            {
                ErrorCode = "AuthorizationError",
                StatusCode = HttpStatusCode.Forbidden
            });

        var result = await new SnsHealthCheck("arn:aws:sns:us-east-1:123:orders", mock.Object).ExecuteAsync();

        // A least-privilege publisher lacking sns:GetTopicAttributes stays green-ish: Warning, not Failed (§3.9).
        Assert.Equal(HealthCheckStatus.Warning, result.Status);
        Assert.Equal("AuthorizationError", result.Data["ErrorCode"]);
        Assert.Equal(403, result.Data["StatusCode"]);
    }
}
