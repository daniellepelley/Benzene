using System.IO;
using System.Threading.Tasks;
using Benzene.HealthChecks.Core;
using Benzene.HealthChecks.Disk;
using Xunit;

namespace Benzene.Test.HealthChecks.Disk;

public class DiskHealthCheckTest
{
    private static readonly string Path = System.IO.Path.GetTempPath();

    [Fact]
    public async Task ExecuteAsync_AmpleFreeSpace_ReturnsHealthy()
    {
        var result = await new DiskHealthCheck(Path, minimumFreeBytes: 0).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("Disk", dependency.Kind);
        Assert.True(result.Data.ContainsKey("FreeBytes"));
    }

    [Fact]
    public async Task ExecuteAsync_BelowMinimum_ReturnsFailed()
    {
        var result = await new DiskHealthCheck(Path, minimumFreeBytes: long.MaxValue).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_BelowWarningButAboveMinimum_ReturnsWarning()
    {
        // Warn threshold impossibly high, min zero -> free space is >= min but < warn -> Warning.
        var result = await new DiskHealthCheck(Path, minimumFreeBytes: 0, warningFreeBytes: long.MaxValue).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Warning, result.Status);
    }
}
