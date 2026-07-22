using System;
using System.Linq;
using Benzene.Abstractions.DI;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Kafka.Core;
using Benzene.Microsoft.Dependencies;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Kafka;

/// <summary>
/// Coverage for Phase 4 Kafka auto-wiring: <c>AddKafkaDependencyHealthCheck</c> (called by
/// <c>UseKafka(..., healthCheck: true)</c>) registers a reachability check on the dependency category
/// (deep <c>healthcheck</c> layer only), deduped by the bootstrap servers. Resolving the check does not
/// build an admin client (the factory is lazy), so no broker connection is attempted.
/// </summary>
public class KafkaAutoWireHealthCheckTest
{
    private sealed class TestRegister : IRegisterDependency
    {
        private readonly IServiceCollection _services;
        public TestRegister(IServiceCollection services) => _services = services;
        public void Register(Action<IBenzeneServiceContainer> action) => action(new MicrosoftBenzeneServiceContainer(_services));
    }

    private static BenzeneKafkaConfig Config(string brokers = "broker:9092") => new()
    {
        ConsumerConfig = new ConsumerConfig { BootstrapServers = brokers, GroupId = "workers" },
        Topics = new[] { "orders" },
    };

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
    public void AddKafkaDependencyHealthCheck_RegistersOnTheDependencyCategoryOnly()
    {
        var finder = Finder(c => c.AddKafkaDependencyHealthCheck(Config()));

        Assert.Single(finder.FindDependencyHealthChecks(), x => x.Type == "Kafka");
        Assert.DoesNotContain(finder.FindHealthChecks(), x => x.Type == "Kafka");
    }

    [Fact]
    public void AddKafkaDependencyHealthCheck_SameBrokersTwice_CollapsesToOneCheck()
    {
        var finder = Finder(c =>
        {
            c.AddKafkaDependencyHealthCheck(Config("broker:9092"));
            c.AddKafkaDependencyHealthCheck(Config("broker:9092"));
        });

        Assert.Single(finder.FindDependencyHealthChecks());
    }
}
