using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using HealthCheckConstants = Benzene.HealthChecks.Constants;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Plugins.HealthChecks;

public class HealthCheckLivenessReadinessTest
{
    private static (BenzeneMessageApplication app, MicrosoftServiceResolverFactory resolverFactory) BuildApp(
        Mock<IHealthCheck> livenessCheck, Mock<IHealthCheck> readinessCheck)
    {
        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage().AddMessageHandlers(typeof(HealthCheckLivenessReadinessTest).Assembly));

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));
        pipeline
            .UseLivenessCheck(livenessCheck.Object)
            .UseReadinessCheck(readinessCheck.Object);

        var app = new BenzeneMessageApplication(pipeline.Build());
        return (app, new MicrosoftServiceResolverFactory(services));
    }

    private static Mock<IHealthCheck> CreateMockCheck(string type, bool healthy = true)
    {
        var mock = new Mock<IHealthCheck>();
        mock.Setup(x => x.Type).Returns(type);
        mock.Setup(x => x.ExecuteAsync())
            .ReturnsAsync(HealthCheckResult.CreateInstance(healthy, type, new Dictionary<string, object>()));
        return mock;
    }

    [Fact]
    public async Task UseLivenessCheck_RespondsToLivenessTopic()
    {
        var livenessCheck = CreateMockCheck("Liveness");
        var readinessCheck = CreateMockCheck("Readiness");
        var (app, resolverFactory) = BuildApp(livenessCheck, readinessCheck);

        var response = await app.HandleAsync(
            new BenzeneMessageRequest { Topic = HealthCheckConstants.DefaultLivenessTopic }, resolverFactory);

        livenessCheck.Verify(x => x.ExecuteAsync(), Times.Once);
        readinessCheck.Verify(x => x.ExecuteAsync(), Times.Never);
        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
    }

    [Fact]
    public async Task UseReadinessCheck_RespondsToReadinessTopic()
    {
        var livenessCheck = CreateMockCheck("Liveness");
        var readinessCheck = CreateMockCheck("Readiness");
        var (app, resolverFactory) = BuildApp(livenessCheck, readinessCheck);

        var response = await app.HandleAsync(
            new BenzeneMessageRequest { Topic = HealthCheckConstants.DefaultReadinessTopic }, resolverFactory);

        readinessCheck.Verify(x => x.ExecuteAsync(), Times.Once);
        livenessCheck.Verify(x => x.ExecuteAsync(), Times.Never);
        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
    }

    [Fact]
    public async Task NeitherLivenessNorReadiness_RespondsToDefaultHealthCheckTopic()
    {
        // Unlike UseHealthCheck, UseLivenessCheck/UseReadinessCheck deliberately do NOT also match
        // HealthCheckConstants.DefaultHealthCheckTopic - otherwise, whichever is registered first would silently
        // shadow the other whenever the generic "healthcheck" topic was requested.
        var livenessCheck = CreateMockCheck("Liveness");
        var readinessCheck = CreateMockCheck("Readiness");
        var (app, resolverFactory) = BuildApp(livenessCheck, readinessCheck);

        await app.HandleAsync(
            new BenzeneMessageRequest { Topic = HealthCheckConstants.DefaultHealthCheckTopic }, resolverFactory);

        livenessCheck.Verify(x => x.ExecuteAsync(), Times.Never);
        readinessCheck.Verify(x => x.ExecuteAsync(), Times.Never);
    }

    [Fact]
    public async Task UseReadinessCheck_Unhealthy_ReturnsServiceUnavailable()
    {
        var livenessCheck = CreateMockCheck("Liveness");
        var readinessCheck = CreateMockCheck("Readiness", healthy: false);
        var (app, resolverFactory) = BuildApp(livenessCheck, readinessCheck);

        var response = await app.HandleAsync(
            new BenzeneMessageRequest { Topic = HealthCheckConstants.DefaultReadinessTopic }, resolverFactory);

        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task UseHealthCheck_Unhealthy_ReturnsServiceUnavailable()
    {
        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage().AddMessageHandlers(typeof(HealthCheckLivenessReadinessTest).Assembly));

        var unhealthyCheck = CreateMockCheck("Database", healthy: false);

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));
        pipeline.UseHealthCheck(HealthCheckConstants.DefaultHealthCheckTopic, unhealthyCheck.Object);

        var app = new BenzeneMessageApplication(pipeline.Build());
        var resolverFactory = new MicrosoftServiceResolverFactory(services);

        var response = await app.HandleAsync(
            new BenzeneMessageRequest { Topic = HealthCheckConstants.DefaultHealthCheckTopic }, resolverFactory);

        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, response.StatusCode);
    }
}
