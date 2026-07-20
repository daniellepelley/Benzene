using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Results;
using Xunit;

namespace Benzene.Test.Plugins.HealthChecks;

public class CachingHealthCheckProcessorTest
{
    private class CountingProcessor : IHealthCheckProcessor
    {
        public int Calls { get; private set; }
        public Task<IBenzeneResult> PerformHealthChecksAsync(IHealthCheck[] healthChecks)
        {
            Calls++;
            return Task.FromResult((IBenzeneResult)BenzeneResult.Ok(new HealthCheckResponse(true,
                new System.Collections.Generic.Dictionary<string, HealthCheckResult>())));
        }
    }

    private static IHealthCheck Check(string type)
    {
        return new InlineHealthCheck(type, () => Task.FromResult(HealthCheckResult.CreateInstance(true, type)));
    }

    [Fact]
    public async Task WithinTtl_ServesFromCache_WithoutReRunningTheChecks()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var inner = new CountingProcessor();
        var processor = new CachingHealthCheckProcessor(inner, TimeSpan.FromSeconds(30), () => now);
        var checks = new[] { Check("a") };

        await processor.PerformHealthChecksAsync(checks);
        now = now.AddSeconds(10);
        await processor.PerformHealthChecksAsync(checks);

        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task AfterTtlExpires_ReRunsTheChecks()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var inner = new CountingProcessor();
        var processor = new CachingHealthCheckProcessor(inner, TimeSpan.FromSeconds(30), () => now);
        var checks = new[] { Check("a") };

        await processor.PerformHealthChecksAsync(checks);
        now = now.AddSeconds(31);
        await processor.PerformHealthChecksAsync(checks);

        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task DifferentCheckSets_CacheIndependently()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var inner = new CountingProcessor();
        var processor = new CachingHealthCheckProcessor(inner, TimeSpan.FromSeconds(30), () => now);

        // Liveness (check "live") and readiness (check "ready") must not share one cache entry.
        await processor.PerformHealthChecksAsync(new[] { Check("live") });
        await processor.PerformHealthChecksAsync(new[] { Check("ready") });

        Assert.Equal(2, inner.Calls);
    }
}
