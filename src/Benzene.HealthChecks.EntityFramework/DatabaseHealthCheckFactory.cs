using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;
using Microsoft.EntityFrameworkCore;

namespace Benzene.HealthChecks.EntityFramework;

public class DatabaseHealthCheckFactory<TDbContext> : IHealthCheckFactory where TDbContext : DbContext
{
    private readonly string _targetMigration;

    public DatabaseHealthCheckFactory(string targetMigration)
    {
        _targetMigration = targetMigration;
    }

    public IHealthCheck Create(IServiceResolver resolver)
    {
        return new DatabaseHealthCheck<TDbContext>(resolver.GetService<TDbContext>(), _targetMigration);
    }
}
