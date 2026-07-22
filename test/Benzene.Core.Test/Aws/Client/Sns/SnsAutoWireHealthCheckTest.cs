using System;
using System.Linq;
using Amazon.SimpleNotificationService;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Clients.Aws.Sns;
using Benzene.Core.Middleware;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Aws.Client.Sns;

/// <summary>
/// Coverage for §3.1/Phase 1 SNS auto-wiring: <c>.UseSns(topicArn)</c> auto-registers a non-destructive
/// reachability check on the <b>dependency</b> category (deep <c>healthcheck</c> layer only — never a
/// Kubernetes probe), reusing the pipeline's own <c>IAmazonSimpleNotificationService</c>.
/// <c>healthCheck: false</c> opts out, and two registrations of the same topic collapse to one check.
/// </summary>
public class SnsAutoWireHealthCheckTest
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
        container.AddScoped(_ => new Mock<IAmazonSimpleNotificationService>().Object);
        _ = new HealthCheckBuilder(new TestRegister(services));

        var app = new MiddlewarePipelineBuilder<IBenzeneClientContext<TestMessage, Void>>(container);
        configure(app);

        var factory = new MicrosoftServiceResolverFactory(services);
        var scope = factory.CreateScope();
        return scope.GetService<IHealthCheckFinder>();
    }

    [Fact]
    public void UseSns_AutoRegistersADependencyCheck_OnTheDependencyCategoryOnly()
    {
        var finder = Finder(app => app.UseSns("arn:aws:sns:eu-west-1:123:orders"));

        Assert.Single(finder.FindDependencyHealthChecks(), x => x.Type == "Sns");
        Assert.DoesNotContain(finder.FindHealthChecks(), x => x.Type == "Sns");
    }

    [Fact]
    public void UseSns_HealthCheckFalse_RegistersNothing()
    {
        var finder = Finder(app => app.UseSns("arn:aws:sns:eu-west-1:123:orders", healthCheck: false));

        Assert.Empty(finder.FindDependencyHealthChecks());
    }

    [Fact]
    public void UseSns_SameTopicTwice_CollapsesToOneCheck()
    {
        var finder = Finder(app =>
        {
            app.UseSns("arn:aws:sns:eu-west-1:123:orders");
            app.UseSns("arn:aws:sns:eu-west-1:123:orders");
        });

        Assert.Single(finder.FindDependencyHealthChecks());
    }
}
