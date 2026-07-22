using System;
using System.Linq;
using Amazon.SQS;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.Middleware;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Aws.Client.Sqs;

/// <summary>
/// Coverage for §3.1/Phase 1 SQS auto-wiring: <c>.UseSqs(queueUrl)</c> auto-registers a non-destructive
/// reachability check on the <b>dependency</b> category (deep <c>healthcheck</c> layer only — never a
/// Kubernetes probe), reusing the pipeline's own <c>IAmazonSQS</c>. <c>healthCheck: false</c> opts out,
/// and two registrations of the same queue collapse to one check.
/// </summary>
public class SqsAutoWireHealthCheckTest
{
    private sealed record TestMessage(string Value);

    private sealed class TestRegister : IRegisterDependency
    {
        private readonly IServiceCollection _services;
        public TestRegister(IServiceCollection services) => _services = services;
        public void Register(Action<IBenzeneServiceContainer> action) => action(new MicrosoftBenzeneServiceContainer(_services));
    }

    private static IHealthCheckFinder Finder(Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<TestMessage, Void>>> configure)
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddScoped(_ => new Mock<IAmazonSQS>().Object);
        // Registers the IHealthCheckFinder that resolves the dependency category.
        _ = new HealthCheckBuilder(new TestRegister(services));

        var app = new MiddlewarePipelineBuilder<IBenzeneClientContext<TestMessage, Void>>(container);
        configure(app);

        var factory = new MicrosoftServiceResolverFactory(services);
        var scope = factory.CreateScope();
        return scope.GetService<IHealthCheckFinder>();
    }

    [Fact]
    public void UseSqs_AutoRegistersADependencyCheck_OnTheDependencyCategoryOnly()
    {
        var finder = Finder(app => app.UseSqs("https://sqs.eu-west-1/123/orders"));

        // On the deep layer...
        Assert.Single(finder.FindDependencyHealthChecks(), x => x.Type == "Sqs");
        // ...and never in the plain set the liveness/readiness probes harvest.
        Assert.DoesNotContain(finder.FindHealthChecks(), x => x.Type == "Sqs");
    }

    [Fact]
    public void UseSqs_HealthCheckFalse_RegistersNothing()
    {
        var finder = Finder(app => app.UseSqs("https://sqs.eu-west-1/123/orders", healthCheck: false));

        Assert.Empty(finder.FindDependencyHealthChecks());
    }

    [Fact]
    public void UseSqs_SameQueueTwice_CollapsesToOneCheck()
    {
        var finder = Finder(app =>
        {
            app.UseSqs("https://sqs.eu-west-1/123/orders");
            app.UseSqs("https://sqs.eu-west-1/123/orders");
        });

        Assert.Single(finder.FindDependencyHealthChecks());
    }
}
