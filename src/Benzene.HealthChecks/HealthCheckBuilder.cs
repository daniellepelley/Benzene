using Benzene.Abstractions.DI;
using Benzene.Core;
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
        // Registered with TryAdd so a consumer can register their own IHealthCheckProcessor first
        // (e.g. with a non-default timeout) and have it win.
        _register.Register(x => x.TryAddSingleton<IHealthCheckProcessor>(_ => new HealthCheckProcessor()));
        // Scoped cancellation-token accessor so a check can observe the ambient token. TryAdd, and
        // mapped so the same scoped instance is resolvable as both the concrete (settable, for a
        // seeder) and the read-only interface (for checks).
        _register.Register(x => x
            .TryAddScoped<CancellationTokenAccessor>()
            .TryAddScoped<ICancellationTokenAccessor>(r => r.GetService<CancellationTokenAccessor>()));
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
        return GetHealthChecks(resolver, includeReadinessScoped: true);
    }

    /// <summary>
    /// Resolves the registered checks for a specific probe scope. The builder-local factory checks and the
    /// plain container-registered checks are always included; the readiness-category checks
    /// (<see cref="IReadinessHealthCheck"/>) are included only when <paramref name="includeReadinessScoped"/>
    /// is <c>true</c> - so a liveness probe never harvests an auto-wired dependency check (§3.2).
    /// </summary>
    /// <param name="resolver">The service resolver used to resolve container-registered checks and to invoke the factory functions.</param>
    /// <param name="includeReadinessScoped">Whether to include the readiness-category checks.</param>
    /// <returns>The health checks for the requested scope: factory-based checks first, then plain container checks, then (optionally) readiness-category checks.</returns>
    public IHealthCheck[] GetHealthChecks(IServiceResolver resolver, bool includeReadinessScoped)
    {
        var healthCheckFinder = resolver.GetService<IHealthCheckFinder>();
        var healthChecks = healthCheckFinder.FindHealthChecks();
        var inlineHealthChecks = _healthCheckBuilders
            .Select(x => new InlineHealthCheck(() => x(resolver).ExecuteAsync())).ToArray();

        var combined = inlineHealthChecks.Concat(healthChecks);
        if (includeReadinessScoped)
        {
            combined = combined.Concat(healthCheckFinder.FindReadinessHealthChecks());
        }

        return combined.ToArray();
    }
}
