using Benzene.HealthChecks.Core;
using Microsoft.EntityFrameworkCore;

namespace Benzene.HealthChecks.EntityFramework;

/// <summary>
/// Checks both that <typeparamref name="TDbContext"/> can connect AND that its schema is on the
/// expected migration - healthy only if the connection succeeds and <paramref name="targetMigration"/>
/// is the LAST applied migration (not merely present among applied migrations). This is a stricter
/// check than <see cref="DatabaseConnectionHealthCheck{TDbContext}"/>: a database that connects fine
/// but hasn't yet had a newer migration applied (or has one newer than expected) reports unhealthy.
/// </summary>
/// <typeparam name="TDbContext">The EF Core context type to check.</typeparam>
public class DatabaseHealthCheck<TDbContext> : IHealthCheck where TDbContext : DbContext
{
    private readonly string _targetMigration;
    private readonly TDbContext _dbContext;

    /// <summary>Initializes a new instance of the <see cref="DatabaseHealthCheck{TDbContext}"/> class.</summary>
    /// <param name="dbContext">The context to check.</param>
    /// <param name="targetMigration">The migration name expected to be the last one applied.</param>
    public DatabaseHealthCheck(TDbContext dbContext, string targetMigration)
    {
        _dbContext = dbContext;
        _targetMigration = targetMigration;
    }

    /// <inheritdoc />
    public string Type => "Database";

    /// <summary>
    /// Checks connectivity and applied migrations. The result's <see cref="IHealthCheckResult.Data"/>
    /// includes <c>CanConnect</c>, <c>AppliedMigrations</c>, <c>TargetMigration</c>,
    /// <c>MigrationMatch</c> (whether the target migration is the last applied one - this drives the
    /// overall pass/fail), <c>MigrationContains</c> (whether it's applied at all, regardless of
    /// position), and <c>Error</c> (the connection exception's message, if any).
    /// </summary>
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

