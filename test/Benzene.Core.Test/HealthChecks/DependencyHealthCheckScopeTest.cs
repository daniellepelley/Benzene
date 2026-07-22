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
/// Coverage for the dependency registration category (§3.2 / Phase 1): a check registered via
/// <see cref="DependencyHealthCheckExtensions.AddDependencyHealthCheck"/> is harvested only by the deep
/// <c>healthcheck</c> probe (<c>includeDependencyChecks: true</c>) and excluded from liveness, readiness
/// and contracts (<c>false</c>) — so a shared-downstream blip can never fail a Kubernetes probe and
/// restart or de-route the fleet. Duplicate registrations of the same dependency collapse to one check.
/// </summary>
public class DependencyHealthCheckScopeTest
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
    public void DependencyCheck_OnlyOnTheDeepHealthcheckLayer_NotTheProbes()
    {
        var services = new ServiceCollection();
        // Constructing the builder registers the IHealthCheckFinder used to resolve container checks.
        var builder = new HealthCheckBuilder(new TestRegister(services));
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddScoped<IHealthCheck>(_ => Check("live"));                    // a plain, probe-eligible self-check
        container.AddDependencyHealthCheck(_ => Check("dep"), "Queue:orders");    // an auto-wired dependency check

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();

        // Liveness/readiness/contracts pass includeDependencyChecks: false; the general healthcheck: true.
        var probeScoped = builder.GetHealthChecks(scope, includeDependencyChecks: false).Select(x => x.Type).ToArray();
        var deepScoped = builder.GetHealthChecks(scope, includeDependencyChecks: true).Select(x => x.Type).ToArray();

        Assert.Contains("live", probeScoped);
        Assert.DoesNotContain("dep", probeScoped);   // the one-way door: never on a probe that takes k8s action

        Assert.Contains("live", deepScoped);
        Assert.Contains("dep", deepScoped);          // surfaced on the deep healthcheck layer
    }

    [Fact]
    public void Finder_KeepsThePlainAndDependencySetsDisjoint()
    {
        var services = new ServiceCollection();
        _ = new HealthCheckBuilder(new TestRegister(services));
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddScoped<IHealthCheck>(_ => Check("live"));
        container.AddDependencyHealthCheck(_ => Check("dep"), "Queue:orders");

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();
        var finder = scope.GetService<IHealthCheckFinder>();

        Assert.DoesNotContain(finder.FindHealthChecks(), x => x.Type == "dep");
        Assert.Contains(finder.FindDependencyHealthChecks(), x => x.Type == "dep");
        Assert.Contains(finder.FindHealthChecks(), x => x.Type == "live");
    }

    [Fact]
    public void DependencyChecks_WithTheSameDedupKey_CollapseToOne()
    {
        var services = new ServiceCollection();
        _ = new HealthCheckBuilder(new TestRegister(services));
        var container = new MicrosoftBenzeneServiceContainer(services);
        // Two registrations of the same dependency (e.g. two .UseSns(sameArn)) - same (Type, Name) key.
        container.AddDependencyHealthCheck(_ => Check("dep"), "Queue:orders");
        container.AddDependencyHealthCheck(_ => Check("dep"), "Queue:orders");
        // A different dependency stays distinct.
        container.AddDependencyHealthCheck(_ => Check("other"), "Queue:invoices");

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();
        var finder = scope.GetService<IHealthCheckFinder>();

        var dependencies = finder.FindDependencyHealthChecks();
        Assert.Equal(2, dependencies.Length);
        Assert.Single(dependencies, x => x.Type == "dep");
        Assert.Single(dependencies, x => x.Type == "other");
    }
}
