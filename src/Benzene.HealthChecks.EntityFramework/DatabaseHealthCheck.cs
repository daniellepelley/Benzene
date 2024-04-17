using Benzene.HealthChecks.Core;
using Microsoft.EntityFrameworkCore;

namespace Benzene.HealthChecks.EntityFramework;

public class DatabaseHealthCheck<TDbContext> : IHealthCheck where TDbContext : DbContext
{
    private readonly string _targetMigration;
    private readonly TDbContext _dbContext;

    public DatabaseHealthCheck(TDbContext dbContext, string targetMigration)
    {
        _dbContext = dbContext;
        _targetMigration = targetMigration;
    }

    public string Type => "Database";

    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var canConnect = await TryConnect(_dbContext);
        var appliedMigrations = await TryGetAppliedMigrationsAsync(_dbContext);

        var migrationContains = appliedMigrations.Contains(_targetMigration);
        var migrationMatch = appliedMigrations.LastOrDefault() == _targetMigration;

        return HealthCheckResult.CreateInstance(canConnect.Item1 && migrationMatch, Type,
            new Dictionary<string, object>
            {
                { "CanConnect", canConnect.Item1 },
                { "AppliedMigrations", appliedMigrations },
                { "TargetMigration", _targetMigration },
                { "MigrationMatch", migrationMatch },
                { "MigrationContains", migrationContains },
                { "Error", canConnect.Item2?.Message }
            });
    }

    private static async Task<(bool, Exception)> TryConnect(DbContext dbContext)
    {
        try
        {
            return (await dbContext.Database.CanConnectAsync(), null);
        }
        catch(Exception ex)
        {
            return (false, ex);
        }
    }

    private static async Task<string[]> TryGetAppliedMigrationsAsync(DbContext dbContext)
    {
        try
        {
            return (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}

