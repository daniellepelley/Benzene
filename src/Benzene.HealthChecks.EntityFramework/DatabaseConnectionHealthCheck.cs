using Benzene.HealthChecks.Core;
using Microsoft.EntityFrameworkCore;

namespace Benzene.HealthChecks.EntityFramework;

public class DatabaseConnectionHealthCheck<TDbContext> : IHealthCheck where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    public string Type => "DatabaseConnection";

    public DatabaseConnectionHealthCheck(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var canConnect = await TryConnect(_dbContext);

        return HealthCheckResult.CreateInstance(canConnect.Item1, Type, new Dictionary<string, object>
        {
            { "CanConnect", canConnect },
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
