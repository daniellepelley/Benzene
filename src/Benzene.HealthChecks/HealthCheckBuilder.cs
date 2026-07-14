using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// Default <see cref="IHealthCheckBuilder"/> implementation. Health checks registered via the
/// <c>THealthCheck</c> overload are registered as scoped services against <see cref="IHealthCheckFinder"/>
/// (constructing an <see cref="HealthCheckFinder"/> and wiring it as a singleton on first use); health
/// checks registered via the factory-function overload are held in-memory and wrapped as
/// <see cref="InlineHealthCheck"/>s at resolution time.
/// </summary>
public class HealthCheckBuilder : IHealthCheckBuilder
{
    private readonly List<Func<IServiceResolver, IHealthCheck>> _healthCheckBuilders = new();
    private readonly IRegisterDependency _register;

    /// <summary>Initializes a new instance of the <see cref="HealthCheckBuilder"/> class, registering the <see cref="IHealthCheckFinder"/> singleton used to discover container-resolved checks.</summary>
    /// <param name="register">The dependency registry checks/services are registered against.</param>
    public HealthCheckBuilder(IRegisterDependency register)
    {
        _register = register;
        _register.Register(x => x.AddSingleton<IHealthCheckFinder, HealthCheckFinder>());
    }

    /// <inheritdoc />
    public IHealthCheckBuilder AddHealthCheck<THealthCheck>() where THealthCheck : class, IHealthCheck
    {
        _register.Register(x => x.AddScoped<IHealthCheck, THealthCheck>());
        return this;
    }

    /// <inheritdoc />
    public IHealthCheckBuilder AddHealthCheck(Func<IServiceResolver, IHealthCheck> func)
    {
        _healthCheckBuilders.Add(func);
        return this;
    }

    /// <summary>
    /// Combines the checks registered via <see cref="AddHealthCheck{THealthCheck}"/> (resolved through
    /// the registered <see cref="IHealthCheckFinder"/>) with the checks registered via
    /// <see cref="AddHealthCheck(Func{IServiceResolver,IHealthCheck})"/> (each wrapped as an
    /// <see cref="InlineHealthCheck"/> so it is not invoked until the aggregated array is executed).
    /// </summary>
    /// <param name="resolver">The service resolver used to resolve container-registered checks and to invoke the factory functions.</param>
    /// <returns>Every registered health check, factory-based checks first, followed by container-resolved checks.</returns>
    public IHealthCheck[] GetHealthChecks(IServiceResolver resolver)
    {
        var healthCheckFinder = resolver.GetService<IHealthCheckFinder>();
        var healthChecks = healthCheckFinder.FindHealthChecks();
        var inlineHealthChecks = _healthCheckBuilders
            .Select(x => new InlineHealthCheck(() => x(resolver).ExecuteAsync())).ToArray();

        return inlineHealthChecks.Concat(healthChecks).ToArray();
    }
}
