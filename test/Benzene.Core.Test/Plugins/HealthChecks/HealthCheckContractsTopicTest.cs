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

// The contracts topic is a diagnostic surface, deliberately separate from the liveness/readiness
// probes (a contract check calls a downstream service and reports drift - it must never be able to
// restart or de-route a pod). These tests pin that separation: UseContractsCheck answers only the
// "contracts" topic, and none of the probe/healthcheck topics trigger it, nor it them.
public class HealthCheckContractsTopicTest
{
    private static (BenzeneMessageApplication app, MicrosoftServiceResolverFactory resolverFactory) BuildApp(
        Mock<IHealthCheck> contractCheck, Mock<IHealthCheck> readinessCheck)
    {
        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage().AddMessageHandlers(typeof(HealthCheckContractsTopicTest).Assembly));

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));
        pipeline
            .UseContractsCheck(contractCheck.Object)
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
    public async Task UseContractsCheck_RespondsToContractsTopic()
    {
        var contractCheck = CreateMockCheck("Contracts");
        var readinessCheck = CreateMockCheck("Readiness");
        var (app, resolverFactory) = BuildApp(contractCheck, readinessCheck);

        var response = await app.HandleAsync(
            new BenzeneMessageRequest { Topic = HealthCheckConstants.DefaultContractsTopic }, resolverFactory);

        contractCheck.Verify(x => x.ExecuteAsync(), Times.Once);
        readinessCheck.Verify(x => x.ExecuteAsync(), Times.Never);
        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
    }

    [Theory]
    [InlineData("readiness")]
    [InlineData("liveness")]
    [InlineData("healthcheck")]
    public async Task UseContractsCheck_DoesNotRespondToProbeOrHealthCheckTopics(string topic)
    {
        // A contract check must not be reachable through a probe topic - that is the whole point of
        // keeping it on its own topic. It also doesn't piggy-back on the generic "healthcheck" topic
        // (matching the liveness/readiness non-shadowing rule).
        var contractCheck = CreateMockCheck("Contracts");
        var readinessCheck = CreateMockCheck("Readiness");
        var (app, resolverFactory) = BuildApp(contractCheck, readinessCheck);

        await app.HandleAsync(new BenzeneMessageRequest { Topic = topic }, resolverFactory);

        contractCheck.Verify(x => x.ExecuteAsync(), Times.Never);
    }

    [Fact]
    public async Task ReadinessProbe_DoesNotRunContractCheck()
    {
        // The reverse guarantee: wiring both in one pipeline never leaks the contract check into the
        // readiness probe's response.
        var contractCheck = CreateMockCheck("Contracts");
        var readinessCheck = CreateMockCheck("Readiness");
        var (app, resolverFactory) = BuildApp(contractCheck, readinessCheck);

        await app.HandleAsync(
            new BenzeneMessageRequest { Topic = HealthCheckConstants.DefaultReadinessTopic }, resolverFactory);

        readinessCheck.Verify(x => x.ExecuteAsync(), Times.Once);
        contractCheck.Verify(x => x.ExecuteAsync(), Times.Never);
    }
}
