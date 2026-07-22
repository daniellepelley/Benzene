using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.HealthChecks;

/// <summary>
/// Coverage for the readiness registration category (§3.2 / Phase 0b-0c): a check registered via
/// <see cref="ReadinessHealthCheckExtensions.AddReadinessHealthCheck"/> is harvested by the general and
/// readiness probes but excluded from liveness, and duplicate registrations of the same dependency
/// collapse to one check.
/// </summary>
public class ReadinessHealthCheckScopeTest
{
    private sealed class TestRegister : IRegisterDependency
    {
        private readonly IServiceCollection _services;
        public TestRegister(IServiceCollection services) => _services = services;
        public void Register(Action<IBenzeneServiceContainer> action) => action(new MicrosoftBenzeneServiceContainer(_services));
    }

    private static IHealthCheck Check(string type) =>
        new InlineHealthCheck(type, () => Task.FromResult(HealthCheckResult.CreateInstance(true, type)));

    [Fact]
    public void ReadinessCheck_ExcludedFromLiveness_ButIncludedInReadinessAndGeneral()
    {
        var services = new ServiceCollection();
        // Constructing the builder registers the IHealthCheckFinder used to resolve container checks.
        var builder = new HealthCheckBuilder(new TestRegister(services));
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddScoped<IHealthCheck>(_ => Check("live"));          // a plain, liveness-eligible self-check
        container.AddReadinessHealthCheck(_ => Check("dep"), "Queue:orders");  // an auto-wired dependency check

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();

        var liveness = builder.GetHealthChecks(scope, includeReadinessScoped: false).Select(x => x.Type).ToArray();
        var readiness = builder.GetHealthChecks(scope, includeReadinessScoped: true).Select(x => x.Type).ToArray();

        Assert.Contains("live", liveness);
        Assert.DoesNotContain("dep", liveness);   // the one-way door: dependency checks never reach liveness

        Assert.Contains("live", readiness);
        Assert.Contains("dep", readiness);
    }

    [Fact]
    public void Finder_KeepsThePlainAndReadinessSetsDisjoint()
    {
        var services = new ServiceCollection();
        _ = new HealthCheckBuilder(new TestRegister(services));
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddScoped<IHealthCheck>(_ => Check("live"));
        container.AddReadinessHealthCheck(_ => Check("dep"), "Queue:orders");

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();
        var finder = scope.GetService<IHealthCheckFinder>();

        Assert.DoesNotContain(finder.FindHealthChecks(), x => x.Type == "dep");
        Assert.Contains(finder.FindReadinessHealthChecks(), x => x.Type == "dep");
        Assert.Contains(finder.FindHealthChecks(), x => x.Type == "live");
    }

    [Fact]
    public void ReadinessChecks_WithTheSameDedupKey_CollapseToOne()
    {
        var services = new ServiceCollection();
        _ = new HealthCheckBuilder(new TestRegister(services));
        var container = new MicrosoftBenzeneServiceContainer(services);
        // Two registrations of the same dependency (e.g. two .UseSns(sameArn)) - same (Type, Name) key.
        container.AddReadinessHealthCheck(_ => Check("dep"), "Queue:orders");
        container.AddReadinessHealthCheck(_ => Check("dep"), "Queue:orders");
        // A different dependency stays distinct.
        container.AddReadinessHealthCheck(_ => Check("other"), "Queue:invoices");

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();
        var finder = scope.GetService<IHealthCheckFinder>();

        var readiness = finder.FindReadinessHealthChecks();
        Assert.Equal(2, readiness.Length);
        Assert.Single(readiness, x => x.Type == "dep");
        Assert.Single(readiness, x => x.Type == "other");
    }
}
