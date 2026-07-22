using System;
using System.Linq;
using Benzene.Abstractions.DI;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.RabbitMq;

/// <summary>
/// Coverage for Phase 4 RabbitMQ auto-wiring: <c>AddRabbitMqDependencyHealthCheck</c> (called by
/// <c>UseRabbitMq(..., healthCheck: true)</c>) registers a reachability check on the dependency category
/// (deep <c>healthcheck</c> layer only), deduped by the queue name. Resolving the check does not open a
/// connection (the provider is lazy).
/// </summary>
public class RabbitMqAutoWireHealthCheckTest
{
    private sealed class TestRegister : IRegisterDependency
    {
        private readonly IServiceCollection _services;
        public TestRegister(IServiceCollection services) => _services = services;
        public void Register(Action<IBenzeneServiceContainer> action) => action(new MicrosoftBenzeneServiceContainer(_services));
    }

    private static RabbitMqConfig Config(string queue = "orders") => new() { QueueName = queue };

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
    public void AddRabbitMqDependencyHealthCheck_RegistersOnTheDependencyCategoryOnly()
    {
        var finder = Finder(c => c.AddRabbitMqDependencyHealthCheck(Config(), Mock.Of<IRabbitMqConnectionFactory>()));

        Assert.Single(finder.FindDependencyHealthChecks(), x => x.Type == "RabbitMq");
        Assert.DoesNotContain(finder.FindHealthChecks(), x => x.Type == "RabbitMq");
    }

    [Fact]
    public void AddRabbitMqDependencyHealthCheck_SameQueueTwice_CollapsesToOneCheck()
    {
        var finder = Finder(c =>
        {
            c.AddRabbitMqDependencyHealthCheck(Config("orders"), Mock.Of<IRabbitMqConnectionFactory>());
            c.AddRabbitMqDependencyHealthCheck(Config("orders"), Mock.Of<IRabbitMqConnectionFactory>());
        });

        Assert.Single(finder.FindDependencyHealthChecks());
    }
}
