using Benzene.Abstractions.DI;

namespace Benzene.HealthChecks.Core
{
    /// <summary>
    /// Builds an <see cref="IHealthCheck"/> instance, given constructor arguments that aren't
    /// themselves resolved from DI (e.g. a URL, a target migration name) - see
    /// <c>Benzene.HealthChecks.Http.HttpPingHealthCheckFactory</c> and
    /// <c>Benzene.HealthChecks.EntityFramework.DatabaseHealthCheckFactory&lt;TDbContext&gt;</c> for
    /// the pattern this exists to support, layered on top via <c>AddHealthCheckFactory</c>.
    /// </summary>
    public interface IHealthCheckFactory
    {
        /// <summary>Creates the health check, resolving any of its dependencies from <paramref name="resolver"/>.</summary>
        /// <param name="resolver">The service resolver used to resolve the check's dependencies.</param>
        IHealthCheck Create(IServiceResolver resolver);
    }
}
