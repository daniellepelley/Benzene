using Benzene.Abstractions.DI;

namespace Benzene.HealthChecks.Core;

public interface IHealthCheckBuilder
{
    IHealthCheckBuilder AddHealthCheck<THealthCheck>() where THealthCheck : class, IHealthCheck;
    IHealthCheckBuilder AddHealthCheck(Func<IServiceResolver, IHealthCheck> func);
    IHealthCheck[] GetHealthChecks(IServiceResolver resolver);
}
