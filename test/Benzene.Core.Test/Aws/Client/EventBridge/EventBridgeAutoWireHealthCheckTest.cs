using System;
using System.Linq;
using Amazon.EventBridge;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Clients.Aws.EventBridge;
using Benzene.Core.Middleware;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Aws.Client.EventBridge;

/// <summary>
/// Coverage for Phase 4 EventBridge auto-wiring: <c>.UseEventBridge&lt;T&gt;(source)</c> auto-registers a
/// non-destructive default-bus reachability check on the dependency category (deep <c>healthcheck</c>
/// layer only), with <c>healthCheck: false</c> opt-out and (Type, Name) dedup.
/// </summary>
public class EventBridgeAutoWireHealthCheckTest
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
        container.AddScoped(_ => new Mock<IAmazonEventBridge>().Object);
        _ = new HealthCheckBuilder(new TestRegister(services));

        var app = new MiddlewarePipelineBuilder<IBenzeneClientContext<TestMessage, Void>>(container);
        configure(app);

        var factory = new MicrosoftServiceResolverFactory(services);
        var scope = factory.CreateScope();
        return scope.GetService<IHealthCheckFinder>();
    }

    [Fact]
    public void UseEventBridge_AutoRegistersADependencyCheck_OnTheDependencyCategoryOnly()
    {
        var finder = Finder(app => app.UseEventBridge<TestMessage>("my.orders.app"));

        Assert.Single(finder.FindDependencyHealthChecks(), x => x.Type == "EventBridge");
        Assert.DoesNotContain(finder.FindHealthChecks(), x => x.Type == "EventBridge");
    }

    [Fact]
    public void UseEventBridge_HealthCheckFalse_RegistersNothing()
    {
        var finder = Finder(app => app.UseEventBridge<TestMessage>("my.orders.app", healthCheck: false));

        Assert.Empty(finder.FindDependencyHealthChecks());
    }

    [Fact]
    public void UseEventBridge_Twice_CollapsesToOneDefaultBusCheck()
    {
        var finder = Finder(app =>
        {
            app.UseEventBridge<TestMessage>("my.orders.app");
            app.UseEventBridge<TestMessage>("my.billing.app");
        });

        // Both target the default bus, so the (Type, Name) dedup collapses them to one check.
        Assert.Single(finder.FindDependencyHealthChecks());
    }
}
