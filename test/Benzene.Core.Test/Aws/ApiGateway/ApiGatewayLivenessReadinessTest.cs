using System.Threading.Tasks;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway.TestHelpers;
using Benzene.Aws.Lambda.Core;
using Benzene.HealthChecks.Core;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Benzene.Testing;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.ApiGateway;

public class ApiGatewayLivenessReadinessTest
{
    [Fact]
    public async Task UseLivenessCheck_RespondsAtDefaultPath()
    {
        var mockHealthCheck = new Mock<IHealthCheck>();
        mockHealthCheck.Setup(x => x.ExecuteAsync()).ReturnsAsync(HealthCheckResult.CreateInstance(true, "Simple"));

        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseLivenessCheck(mockHealthCheck.Object)))
            .BuildHost();

        var request = HttpBuilder.Create("GET", "/livez").AsApiGatewayRequest();
        var response = await host.SendApiGatewayAsync(request);

        Assert.Equal(200, response.StatusCode);
        mockHealthCheck.Verify(x => x.ExecuteAsync(), Times.Once);
    }

    [Fact]
    public async Task UseReadinessCheck_RespondsAtDefaultPath()
    {
        var mockHealthCheck = new Mock<IHealthCheck>();
        mockHealthCheck.Setup(x => x.ExecuteAsync()).ReturnsAsync(HealthCheckResult.CreateInstance(true, "Database"));

        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseReadinessCheck(mockHealthCheck.Object)))
            .BuildHost();

        var request = HttpBuilder.Create("GET", "/readyz").AsApiGatewayRequest();
        var response = await host.SendApiGatewayAsync(request);

        Assert.Equal(200, response.StatusCode);
        mockHealthCheck.Verify(x => x.ExecuteAsync(), Times.Once);
    }

    [Fact]
    public async Task UseReadinessCheck_ReportsUnhealthy_AsNonSuccessStatus()
    {
        var mockHealthCheck = new Mock<IHealthCheck>();
        mockHealthCheck.Setup(x => x.ExecuteAsync()).ReturnsAsync(HealthCheckResult.CreateInstance(false, "Database"));

        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseReadinessCheck(mockHealthCheck.Object)))
            .BuildHost();

        var request = HttpBuilder.Create("GET", "/readyz").AsApiGatewayRequest();
        var response = await host.SendApiGatewayAsync(request);

        Assert.NotEqual(200, response.StatusCode);
    }

    [Fact]
    public async Task LivenessAndReadiness_AreIndependent_NeitherShadowsTheOther()
    {
        var livenessCheck = new Mock<IHealthCheck>();
        livenessCheck.Setup(x => x.ExecuteAsync()).ReturnsAsync(HealthCheckResult.CreateInstance(true, "Liveness"));
        var readinessCheck = new Mock<IHealthCheck>();
        readinessCheck.Setup(x => x.ExecuteAsync()).ReturnsAsync(HealthCheckResult.CreateInstance(true, "Readiness"));

        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseLivenessCheck(livenessCheck.Object)
                    .UseReadinessCheck(readinessCheck.Object)))
            .BuildHost();

        await host.SendApiGatewayAsync(HttpBuilder.Create("GET", "/livez").AsApiGatewayRequest());
        livenessCheck.Verify(x => x.ExecuteAsync(), Times.Once);
        readinessCheck.Verify(x => x.ExecuteAsync(), Times.Never);

        await host.SendApiGatewayAsync(HttpBuilder.Create("GET", "/readyz").AsApiGatewayRequest());
        readinessCheck.Verify(x => x.ExecuteAsync(), Times.Once);
        livenessCheck.Verify(x => x.ExecuteAsync(), Times.Once);
    }

    [Fact]
    public async Task UseLivenessCheck_CustomPath()
    {
        var mockHealthCheck = new Mock<IHealthCheck>();
        mockHealthCheck.Setup(x => x.ExecuteAsync()).ReturnsAsync(HealthCheckResult.CreateInstance(true, "Simple"));

        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseLivenessCheck("/healthz/live", mockHealthCheck.Object)))
            .BuildHost();

        var response = await host.SendApiGatewayAsync(HttpBuilder.Create("GET", "/healthz/live").AsApiGatewayRequest());

        Assert.Equal(200, response.StatusCode);
    }
}
