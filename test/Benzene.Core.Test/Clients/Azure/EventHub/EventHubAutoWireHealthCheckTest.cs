using System;
using System.Linq;
using Azure.Messaging.EventHubs.Producer;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Clients.Azure.EventHub;
using Benzene.Core.Middleware;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients.Azure.EventHub;

/// <summary>
/// Coverage for Phase 4 Event Hubs auto-wiring: <c>.UseEventHub&lt;T&gt;(producerClient)</c> auto-registers a
/// non-destructive reachability check on the dependency category (deep <c>healthcheck</c> layer only),
/// capturing the caller's <c>EventHubProducerClient</c> instance, with <c>healthCheck: false</c> opt-out
/// and (Type, Name) dedup by the hub name.
/// </summary>
public class EventHubAutoWireHealthCheckTest
{
    private sealed record TestMessage(string Value);

    private sealed class TestRegister : IRegisterDependency
    {
        private readonly IServiceCollection _services;
        public TestRegister(IServiceCollection services) => _services = services;
        public void Register(Action<IBenzeneServiceContainer> action) => action(new MicrosoftBenzeneServiceContainer(_services));
    }

    // A real producer client (EventHubName is non-virtual, so it can't be mocked) built from a fake
    // connection string: construction is lazy (no network), and the auto-wire path only registers the
    // check, never executes it. EventHubName is parsed from the EntityPath.
    private static EventHubProducerClient Producer(string hub) =>
        new($"Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=c2VjcmV0;EntityPath={hub}");

    private static IHealthCheckFinder Finder(Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<TestMessage, Void>>> configure)
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        _ = new HealthCheckBuilder(new TestRegister(services));

        var app = new MiddlewarePipelineBuilder<IBenzeneClientContext<TestMessage, Void>>(container);
        configure(app);

        var factory = new MicrosoftServiceResolverFactory(services);
        var scope = factory.CreateScope();
        return scope.GetService<IHealthCheckFinder>();
    }

    [Fact]
    public void UseEventHub_AutoRegistersADependencyCheck_OnTheDependencyCategoryOnly()
    {
        var finder = Finder(app => app.UseEventHub<TestMessage>(Producer("orders-hub")));

        Assert.Single(finder.FindDependencyHealthChecks(), x => x.Type == "EventHub");
        Assert.DoesNotContain(finder.FindHealthChecks(), x => x.Type == "EventHub");
    }

    [Fact]
    public void UseEventHub_HealthCheckFalse_RegistersNothing()
    {
        var finder = Finder(app => app.UseEventHub<TestMessage>(Producer("orders-hub"), healthCheck: false));

        Assert.Empty(finder.FindDependencyHealthChecks());
    }

    [Fact]
    public void UseEventHub_SameHubTwice_CollapsesToOneCheck()
    {
        var finder = Finder(app =>
        {
            app.UseEventHub<TestMessage>(Producer("orders-hub"));
            app.UseEventHub<TestMessage>(Producer("orders-hub"));
        });

        Assert.Single(finder.FindDependencyHealthChecks());
    }
}
