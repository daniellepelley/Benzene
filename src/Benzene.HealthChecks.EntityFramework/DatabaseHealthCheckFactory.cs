using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;
using Microsoft.EntityFrameworkCore;

namespace Benzene.HealthChecks.EntityFramework;

/// <summary>
/// Builds a <see cref="DatabaseHealthCheck{TDbContext}"/> for a fixed target migration, resolving
/// <typeparamref name="TDbContext"/> from DI each time the check runs.
/// </summary>
/// <typeparam name="TDbContext">The EF Core context type to check.</typeparam>
public class DatabaseHealthCheckFactory<TDbContext> : IHealthCheckFactory where TDbContext : DbContext
{
    private readonly string _targetMigration;

    /// <summary>Initializes a new instance of the <see cref="DatabaseHealthCheckFactory{TDbContext}"/> class.</summary>
    /// <param name="targetMigration">The migration name the resulting health check expects to be the last one applied.</param>
    public DatabaseHealthCheckFactory(string targetMigration)
    {
        _targetMigration = targetMigration;
    }

    /// <inheritdoc />
    public IHealthCheck Create(IServiceResolver resolver)
    {
        return new DatabaseHealthCheck<TDbContext>(resolver.GetService<TDbContext>(), _targetMigration);
    }
}
