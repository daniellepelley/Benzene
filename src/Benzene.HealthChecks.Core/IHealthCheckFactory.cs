using Benzene.Abstractions.DI;

namespace Benzene.HealthChecks.Core
{
    public interface IHealthCheckFactory
    {
        IHealthCheck Create(IServiceResolver resolver);
    }
}
