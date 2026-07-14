using Benzene.HealthChecks.Core;
using Microsoft.EntityFrameworkCore;

namespace Benzene.HealthChecks.EntityFramework;

/// <summary>
/// Checks only that <typeparamref name="TDbContext"/> can connect - unlike
/// <see cref="DatabaseHealthCheck{TDbContext}"/>, it does not also verify the applied migration.
/// </summary>
/// <typeparam name="TDbContext">The EF Core context type to check.</typeparam>
public class DatabaseConnectionHealthCheck<TDbContext> : IHealthCheck where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    /// <inheritdoc />
    public string Type => "DatabaseConnection";

    /// <summary>Initializes a new instance of the <see cref="DatabaseConnectionHealthCheck{TDbContext}"/> class.</summary>
    /// <param name="dbContext">The context to check.</param>
    public DatabaseConnectionHealthCheck(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Checks connectivity via <c>DbContext.Database.CanConnectAsync</c>. The result's
    /// <see cref="IHealthCheckResult.Data"/> includes <c>CanConnect</c> and <c>Error</c> (the
    /// connection exception's message, if any).
    /// </summary>
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var canConnect = await TryConnect(_dbContext);

        return HealthCheckResult.CreateInstance(canConnect.Item1, Type, new Dictionary<string, object>
        {
            { "CanConnect", canConnect.Item1 },
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
}
