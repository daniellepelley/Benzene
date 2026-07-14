using System;
using System.Threading.Tasks;
using Benzene.HealthChecks.Core;
using Benzene.HealthChecks.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Benzene.Test.HealthChecks.EntityFramework;

public class DatabaseConnectionHealthCheckTest
{
    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsHealthy_WhenCanConnect()
    {
        using var context = CreateContext();
        var healthCheck = new DatabaseConnectionHealthCheck<TestDbContext>(context);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("DatabaseConnection", result.Type);
        Assert.Equal(true, result.Data["CanConnect"]);
        Assert.Null(result.Data["Error"]);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailed_WhenConnectionThrows()
    {
        var context = CreateContext();
        await context.DisposeAsync();
        var healthCheck = new DatabaseConnectionHealthCheck<TestDbContext>(context);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal(false, result.Data["CanConnect"]);
        Assert.NotNull(result.Data["Error"]);
    }

    [Fact]
    public void Type_IsDatabaseConnection()
    {
        using var context = CreateContext();
        var healthCheck = new DatabaseConnectionHealthCheck<TestDbContext>(context);

        Assert.Equal("DatabaseConnection", healthCheck.Type);
    }
}
