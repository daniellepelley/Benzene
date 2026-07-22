using System;
using System.Threading.Tasks;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Xunit;

namespace Benzene.Test.HealthChecks;

/// <summary>
/// Coverage for the per-check overrides the processor honours (§3.4 / §3.5): a non-critical check's
/// failure degrades rather than flips the probe, and a check's own <see cref="IHealthCheck.Timeout"/>
/// replaces the processor-wide timeout.
/// </summary>
public class HealthCheckProcessorTest
{
    // A check with a fixed outcome and a configurable IsNonCritical override.
    private sealed class StubCheck : IHealthCheck
    {
        private readonly bool _ok;
        public StubCheck(string type, bool ok, bool isNonCritical) { Type = type; _ok = ok; IsNonCritical = isNonCritical; }
        public string Type { get; }
        public bool IsNonCritical { get; }
        public Task<IHealthCheckResult> ExecuteAsync() => Task.FromResult(HealthCheckResult.CreateInstance(_ok, Type));
    }

    // A check that takes _delay to complete, with a configurable Timeout override.
    private sealed class SlowCheck : IHealthCheck
    {
        private readonly TimeSpan _delay;
        public SlowCheck(string type, TimeSpan delay, TimeSpan? timeout) { Type = type; _delay = delay; Timeout = timeout; }
        public string Type { get; }
        public TimeSpan? Timeout { get; }
        public async Task<IHealthCheckResult> ExecuteAsync()
        {
            await Task.Delay(_delay);
            return HealthCheckResult.CreateInstance(true, Type);
        }
    }

    private static async Task<HealthCheckResponse> RunAsync(HealthCheckProcessor processor, params IHealthCheck[] checks)
    {
        var result = await processor.PerformHealthChecksAsync(checks);
        return (HealthCheckResponse)result.PayloadAsObject;
    }

    [Fact]
    public async Task NonCriticalFailure_DoesNotFlipHealthy_AndIsReportedAsWarning()
    {
        var response = await RunAsync(new HealthCheckProcessor(), new StubCheck("dep", ok: false, isNonCritical: true));

        // A non-critical dependency being down degrades the instance but does not take it out of service.
        Assert.True(response.IsHealthy);
        Assert.Equal(HealthCheckStatus.Warning, response.HealthChecks["dep"].Status);
    }

    [Fact]
    public async Task CriticalFailure_FlipsUnhealthy()
    {
        // isNonCritical: false is also the default - a failing critical check flips the probe unhealthy.
        var response = await RunAsync(new HealthCheckProcessor(), new StubCheck("dep", ok: false, isNonCritical: false));

        Assert.False(response.IsHealthy);
        Assert.Equal(HealthCheckStatus.Failed, response.HealthChecks["dep"].Status);
    }

    [Fact]
    public async Task DependencyCategoryCheck_ThatFails_DegradesToWarning_KeepingTheEndpointHealthy()
    {
        // The inner check reports Failed and even declares itself critical, but the dependency category
        // forces non-critical: a down dependency degrades the deep healthcheck report (Warning) rather
        // than flipping the endpoint to 503. This is what keeps a healthcheck endpoint green when an
        // auto-wired downstream (e.g. an egress queue) is unreachable — the common integration-test case.
        var wrapped = new DependencyHealthCheck(new StubCheck("dep", ok: false, isNonCritical: false));

        var response = await RunAsync(new HealthCheckProcessor(), wrapped);

        Assert.True(response.IsHealthy);
        Assert.Equal(HealthCheckStatus.Warning, response.HealthChecks["dep"].Status);
    }

    [Fact]
    public async Task PerCheckTimeout_ShorterThanProcessor_TimesTheCheckOut()
    {
        // Processor budget is generous (30s); the check's own 10ms Timeout is what should bite.
        var processor = new HealthCheckProcessor(TimeSpan.FromSeconds(30));
        var response = await RunAsync(processor, new SlowCheck("slow", TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(10)));

        Assert.False(response.IsHealthy);
        Assert.Equal("Timed Out", response.HealthChecks["slow"].Data["Error"]);
    }

    [Fact]
    public async Task PerCheckTimeout_LongerThanProcessor_LetsTheSlowCheckPass()
    {
        // Processor budget is tight (10ms); the check's own 5s Timeout override widens it so a 200ms
        // check still passes - proving the override replaces the processor-wide timeout in both directions.
        var processor = new HealthCheckProcessor(TimeSpan.FromMilliseconds(10));
        var response = await RunAsync(processor, new SlowCheck("slow", TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(5)));

        Assert.True(response.IsHealthy);
    }
}
