using System;
using System.Threading.Tasks;
using Benzene.HealthChecks.Core;
using Benzene.HealthChecks.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Benzene.Test.HealthChecks.EntityFramework;

// The EF Core InMemory provider (used here to avoid a real database dependency in tests) does not
// support relational-only APIs like Database.GetAppliedMigrationsAsync() - it always throws
// InvalidOperationException, which DatabaseHealthCheck's TryGetAppliedMigrationsAsync catches and
// treats as "no migrations applied". That means every InMemory-backed scenario below reports
// MigrationMatch = false regardless of connectivity - there is no way to exercise the "connects AND
// migration matches" healthy path without a real relational provider (SQL Server/SQLite/Postgres).
public class DatabaseHealthCheckTest
{
    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailed_WhenMigrationsCannotBeDetermined()
    {
        using var context = CreateContext();
        var healthCheck = new DatabaseHealthCheck<TestDbContext>(context, "20260101000000_Initial");

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal("Database", result.Type);
        Assert.Equal(true, result.Data["CanConnect"]);
        Assert.Equal(Array.Empty<string>(), result.Data["AppliedMigrations"]);
        Assert.Equal("20260101000000_Initial", result.Data["TargetMigration"]);
        Assert.Equal(false, result.Data["MigrationMatch"]);
        Assert.Equal(false, result.Data["MigrationContains"]);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailed_WhenConnectionThrows()
    {
        var context = CreateContext();
        await context.DisposeAsync();
        var healthCheck = new DatabaseHealthCheck<TestDbContext>(context, "20260101000000_Initial");

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal(false, result.Data["CanConnect"]);
        Assert.NotNull(result.Data["Error"]);
    }

    [Fact]
    public void Type_IsDatabase()
    {
        using var context = CreateContext();
        var healthCheck = new DatabaseHealthCheck<TestDbContext>(context, "20260101000000_Initial");

        Assert.Equal("Database", healthCheck.Type);
    }
}
