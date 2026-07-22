using System;
using System.Linq;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;
using Benzene.Azure.ServiceBus;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Azure.ServiceBusWorker;

/// <summary>
/// Coverage for Phase 4 Service Bus <b>consumer-side</b> auto-wiring: <c>AddServiceBusDependencyHealthCheck</c>
/// (called by <c>UseServiceBus(..., healthCheck: true)</c>) registers the peek-based reachability check on
/// the dependency category (deep <c>healthcheck</c> layer only), deduped by the consumed entity. Wiring is
/// on the consumer (which holds the Listen claim), never the sender.
/// </summary>
public class ServiceBusAutoWireHealthCheckTest
{
    private sealed class TestRegister : IRegisterDependency
    {
        private readonly IServiceCollection _services;
        public TestRegister(IServiceCollection services) => _services = services;
        public void Register(Action<IBenzeneServiceContainer> action) => action(new MicrosoftBenzeneServiceContainer(_services));
    }

    private static IServiceBusClientFactory Factory()
    {
        // Construction is lazy (no connection), so a fake connection string is fine — the check is never executed here.
        var client = new ServiceBusClient("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=c2VjcmV0");
        return Mock.Of<IServiceBusClientFactory>(f => f.Create() == client);
    }

    private static IHealthCheckFinder Finder(Action<IBenzeneServiceContainer> register)
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        _ = new HealthCheckBuilder(new TestRegister(services));
        register(container);

        var factory = new MicrosoftServiceResolverFactory(services);
        var scope = factory.CreateScope();
        return scope.GetService<IHealthCheckFinder>();
    }

    [Fact]
    public void AddServiceBusDependencyHealthCheck_Queue_RegistersOnTheDependencyCategoryOnly()
    {
        var finder = Finder(c => c.AddServiceBusDependencyHealthCheck(new BenzeneServiceBusConfig { QueueName = "orders" }, Factory()));

        Assert.Single(finder.FindDependencyHealthChecks(), x => x.Type == "ServiceBus");
        Assert.DoesNotContain(finder.FindHealthChecks(), x => x.Type == "ServiceBus");
    }

    [Fact]
    public void AddServiceBusDependencyHealthCheck_Subscription_Registers()
    {
        var finder = Finder(c => c.AddServiceBusDependencyHealthCheck(
            new BenzeneServiceBusConfig { TopicName = "orders", SubscriptionName = "billing" }, Factory()));

        Assert.Single(finder.FindDependencyHealthChecks(), x => x.Type == "ServiceBus");
    }

    [Fact]
    public void AddServiceBusDependencyHealthCheck_SameEntityTwice_CollapsesToOneCheck()
    {
        var finder = Finder(c =>
        {
            c.AddServiceBusDependencyHealthCheck(new BenzeneServiceBusConfig { QueueName = "orders" }, Factory());
            c.AddServiceBusDependencyHealthCheck(new BenzeneServiceBusConfig { QueueName = "orders" }, Factory());
        });

        Assert.Single(finder.FindDependencyHealthChecks());
    }
}
