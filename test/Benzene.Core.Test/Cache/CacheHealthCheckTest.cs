using System;
using System.Threading.Tasks;
using Benzene.Cache.Core;
using Benzene.HealthChecks.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Cache;

public class CacheHealthCheckTest
{
    [Fact]
    public async Task ExecuteAsync_ReturnsHealthy_WhenCanConnect()
    {
        var mockCacheService = new Mock<ICacheService>();
        mockCacheService.Setup(x => x.CanConnectAsync()).ReturnsAsync(true);

        var healthCheck = new CacheHealthCheck<ICacheService>(mockCacheService.Object, NullLogger<CacheHealthCheck<ICacheService>>.Instance);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("Cache", healthCheck.Type);
        Assert.Equal(true, result.Data["CanConnect"]);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("Cache", dependency.Kind);
        Assert.Equal(nameof(ICacheService), dependency.Name);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailed_WhenConnectionThrows()
    {
        var mockCacheService = new Mock<ICacheService>();
        mockCacheService.Setup(x => x.CanConnectAsync()).ThrowsAsync(new InvalidOperationException("connection string with a secret in it"));

        var healthCheck = new CacheHealthCheck<ICacheService>(mockCacheService.Object, NullLogger<CacheHealthCheck<ICacheService>>.Instance);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal(false, result.Data["CanConnect"]);
        Assert.Equal(nameof(InvalidOperationException), result.Data["Error"]);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("Cache", dependency.Kind);
    }
}
