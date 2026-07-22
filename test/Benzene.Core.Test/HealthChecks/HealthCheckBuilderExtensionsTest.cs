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
/// Coverage for the "bring your own check" lambda helpers: a non-destructive probe delegate becomes a
/// full <see cref="IHealthCheck"/> that reports Ok/Failed and carries a <see cref="HealthCheckDependency"/>,
/// without the caller writing a class. See <c>work/client-health-checks-design.md</c> §3.8.
/// </summary>
public class HealthCheckBuilderExtensionsTest
{
    // Applies each Register action against a container over the shared collection, so the builder's
    // own IHealthCheckFinder registration lands somewhere the resolver can later see.
    private sealed class TestRegister : IRegisterDependency
    {
        private readonly IServiceCollection _services;
        public TestRegister(IServiceCollection services) => _services = services;
        public void Register(Action<IBenzeneServiceContainer> action) => action(new MicrosoftBenzeneServiceContainer(_services));
    }

    private static async Task<IHealthCheckResult> RunSingle(Action<IHealthCheckBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = new HealthCheckBuilder(new TestRegister(services));
        configure(builder);

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();
        var check = builder.GetHealthChecks(scope).Single();
        return await check.ExecuteAsync();
    }

    [Fact]
    public async Task LambdaProbe_ReturnsNormally_IsOk_AndCarriesTheDependency()
    {
        var result = await RunSingle(b => b.AddHealthCheck("Database", "orders-db", () => Task.CompletedTask));

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("orders-db", result.Type);
        Assert.Contains(result.Dependencies, d => d.Kind == "Database" && d.Name == "orders-db");
    }

    [Fact]
    public async Task LambdaProbe_Throws_IsFailed_AndReportsErrorTypeNotMessage()
    {
        var result = await RunSingle(b =>
            b.AddHealthCheck("Http", "partner-api",
                () => throw new InvalidOperationException("host=secret;password=hunter2")));

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal("InvalidOperationException", result.Data["Error"]);
        // Secret-safety: the exception message must never reach the result.
        Assert.DoesNotContain("hunter2", result.Data["Error"].ToString());
        Assert.Contains(result.Dependencies, d => d.Kind == "Http" && d.Name == "partner-api");
    }

    [Fact]
    public async Task BoolProbe_False_IsFailed()
    {
        var result = await RunSingle(b => b.AddHealthCheck("Http", "partner-api", () => Task.FromResult(false)));

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal("partner-api", result.Type);
    }

    [Fact]
    public async Task BoolProbe_True_IsOk()
    {
        var result = await RunSingle(b => b.AddHealthCheck("Http", "partner-api", () => Task.FromResult(true)));

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
    }
}
