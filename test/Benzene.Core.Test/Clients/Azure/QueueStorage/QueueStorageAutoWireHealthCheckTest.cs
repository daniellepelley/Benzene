using System;
using System.Linq;
using Azure.Storage.Queues;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Clients.Azure.QueueStorage;
using Benzene.Core.Middleware;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients.Azure.QueueStorage;

/// <summary>
/// Coverage for Phase 4 Queue Storage auto-wiring: <c>.UseQueueStorage&lt;T&gt;(queueClient)</c> auto-registers
/// a non-destructive reachability check on the dependency category (deep <c>healthcheck</c> layer only),
/// capturing the caller's <c>QueueClient</c> instance directly, with <c>healthCheck: false</c> opt-out and
/// (Type, Name) dedup by the queue name.
/// </summary>
public class QueueStorageAutoWireHealthCheckTest
{
    private sealed record TestMessage(string Value);

    private sealed class TestRegister : IRegisterDependency
    {
        private readonly IServiceCollection _services;
        public TestRegister(IServiceCollection services) => _services = services;
        public void Register(Action<IBenzeneServiceContainer> action) => action(new MicrosoftBenzeneServiceContainer(_services));
    }

    private static QueueClient QueueClient(string name) =>
        Mock.Of<QueueClient>(x => x.Name == name);

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
    public void UseQueueStorage_AutoRegistersADependencyCheck_OnTheDependencyCategoryOnly()
    {
        var finder = Finder(app => app.UseQueueStorage<TestMessage>(QueueClient("orders")));

        Assert.Single(finder.FindDependencyHealthChecks(), x => x.Type == "QueueStorage");
        Assert.DoesNotContain(finder.FindHealthChecks(), x => x.Type == "QueueStorage");
    }

    [Fact]
    public void UseQueueStorage_HealthCheckFalse_RegistersNothing()
    {
        var finder = Finder(app => app.UseQueueStorage<TestMessage>(QueueClient("orders"), healthCheck: false));

        Assert.Empty(finder.FindDependencyHealthChecks());
    }

    [Fact]
    public void UseQueueStorage_SameQueueTwice_CollapsesToOneCheck()
    {
        var finder = Finder(app =>
        {
            app.UseQueueStorage<TestMessage>(QueueClient("orders"));
            app.UseQueueStorage<TestMessage>(QueueClient("orders"));
        });

        Assert.Single(finder.FindDependencyHealthChecks());
    }
}
