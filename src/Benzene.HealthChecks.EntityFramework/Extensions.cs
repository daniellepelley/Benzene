using Benzene.HealthChecks.Core;
using Microsoft.EntityFrameworkCore;

namespace Benzene.HealthChecks.EntityFramework;

/// <summary>Registration helpers for the Entity Framework Core health checks, for parity with the other providers' <c>Add*</c> extensions.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers a <see cref="DatabaseHealthCheck{TDbContext}"/> that verifies connectivity AND that
    /// <paramref name="targetMigration"/> is the last applied migration. <typeparamref name="TDbContext"/>
    /// is resolved from DI each time the check runs.
    /// </summary>
    /// <typeparam name="TDbContext">The EF Core context type to check.</typeparam>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="targetMigration">The full migration id expected to be the last one applied (e.g. <c>20260101000000_Initial</c>).</param>
    public static IHealthCheckBuilder AddDatabaseHealthCheck<TDbContext>(this IHealthCheckBuilder builder, string targetMigration)
        where TDbContext : DbContext
    {
        return builder.AddHealthCheckFactory(new DatabaseHealthCheckFactory<TDbContext>(targetMigration));
    }

    /// <summary>
    /// Registers a <see cref="DatabaseConnectionHealthCheck{TDbContext}"/> that verifies only that
    /// <typeparamref name="TDbContext"/> can connect (no migration check). The context is resolved
    /// from DI each time the check runs.
    /// </summary>
    /// <typeparam name="TDbContext">The EF Core context type to check.</typeparam>
    /// <param name="builder">The health check builder to register against.</param>
    public static IHealthCheckBuilder AddDatabaseConnectionHealthCheck<TDbContext>(this IHealthCheckBuilder builder)
        where TDbContext : DbContext
    {
        return builder.AddHealthCheck(resolver => new DatabaseConnectionHealthCheck<TDbContext>(resolver.GetService<TDbContext>()));
    }
}
