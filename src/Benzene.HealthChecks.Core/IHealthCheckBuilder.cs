using Benzene.Abstractions.DI;

namespace Benzene.HealthChecks.Core;

/// <summary>
/// Fluent builder for registering the set of <see cref="IHealthCheck"/>s a health-check
/// endpoint/topic runs. See <see cref="HealthCheckBuilderExtensions"/> for the additional
/// instance/factory-based overloads layered on top of the two members here.
/// </summary>
public interface IHealthCheckBuilder
{
    /// <summary>Registers a health check, resolving <typeparamref name="THealthCheck"/> from the container each time checks run.</summary>
    /// <typeparam name="THealthCheck">The health check type to resolve and run.</typeparam>
    IHealthCheckBuilder AddHealthCheck<THealthCheck>() where THealthCheck : class, IHealthCheck;

    /// <summary>Registers a health check via a factory function invoked with the current <see cref="IServiceResolver"/> each time checks run.</summary>
    /// <param name="func">Produces the health check instance to run.</param>
    IHealthCheckBuilder AddHealthCheck(Func<IServiceResolver, IHealthCheck> func);

    /// <summary>Resolves every registered health check against the given resolver.</summary>
    /// <param name="resolver">The service resolver used to construct/resolve each registered check.</param>
    IHealthCheck[] GetHealthChecks(IServiceResolver resolver);
}
