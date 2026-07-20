using System.Threading.Tasks;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Xunit;

namespace Benzene.Test.HealthChecks;

public class MemoryHealthCheckTest
{
    private static MemoryHealthCheck WithWorkingSet(long bytes, long maximumBytes, long? warningBytes = null)
        => new(maximumBytes, warningBytes, () => bytes);

    [Fact]
    public async Task ExecuteAsync_BelowThresholds_ReturnsHealthy()
    {
        var result = await WithWorkingSet(100, maximumBytes: 1000, warningBytes: 500).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("Memory", result.Type);
        Assert.Equal(100L, result.Data["WorkingSetBytes"]);
        Assert.True(result.Data.ContainsKey("ManagedHeapBytes"));
    }

    [Fact]
    public async Task ExecuteAsync_AtOrAboveMaximum_ReturnsFailed()
    {
        var result = await WithWorkingSet(1000, maximumBytes: 1000).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_AtOrAboveWarningButBelowMaximum_ReturnsWarning()
    {
        var result = await WithWorkingSet(600, maximumBytes: 1000, warningBytes: 500).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Warning, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_NoWarningThreshold_BelowMaximumIsHealthy()
    {
        var result = await WithWorkingSet(999, maximumBytes: 1000).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_RealWorkingSet_IsMeasuredAndHealthyUnderAmpleCeiling()
    {
        // The default ctor reads the real Environment.WorkingSet; long.MaxValue can't be exceeded,
        // so a live process is healthy and the measured value is a positive number.
        var result = await new MemoryHealthCheck(maximumBytes: long.MaxValue).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.True((long)result.Data["WorkingSetBytes"] > 0);
    }
}
