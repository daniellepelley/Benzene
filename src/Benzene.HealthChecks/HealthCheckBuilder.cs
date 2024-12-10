using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

public class HealthCheckBuilder : IHealthCheckBuilder
{
    private readonly List<Func<IServiceResolver, IHealthCheck>> _healthCheckBuilders = new();
    private readonly IRegisterDependency _register;

    public HealthCheckBuilder(IRegisterDependency register)
    {
        _register = register;
        _register.Register(x => x.AddSingleton<IHealthCheckFinder, HealthCheckFinder>());
    }

    public IHealthCheckBuilder AddHealthCheck<THealthCheck>() where THealthCheck : class, IHealthCheck
    {
        _register.Register(x => x.AddScoped<IHealthCheck, THealthCheck>());
        return this;
    }

    public IHealthCheckBuilder AddHealthCheck(Func<IServiceResolver, IHealthCheck> func)
    {
        _healthCheckBuilders.Add(func);
        return this;
    }

    public IHealthCheck[] GetHealthChecks(IServiceResolver resolver)
    {
        var healthCheckFinder = resolver.GetService<IHealthCheckFinder>();
        var healthChecks = healthCheckFinder.FindHealthChecks();
        var inlineHealthChecks = _healthCheckBuilders
            .Select(x => new InlineHealthCheck(() => x(resolver).ExecuteAsync())).ToArray();
        
        return inlineHealthChecks.Concat(healthChecks).ToArray();
    }
}
