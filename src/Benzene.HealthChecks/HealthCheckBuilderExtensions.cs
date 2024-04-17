using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

public static class HealthCheckBuilderExtensions
{
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, string type, Func<IServiceResolver, IHealthCheckResult> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(type, () => Task.FromResult(func(x))));
    }

    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, string type, Func<IServiceResolver, Task<IHealthCheckResult>> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(type, () => func(x)));
    }
    
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, Func<IServiceResolver, IHealthCheckResult> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(() => Task.FromResult(func(x))));
    }

    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, Func<IServiceResolver, Task<IHealthCheckResult>> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(() => func(x)));
    }
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, string type, Func<IServiceResolver, bool> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(type, () => Task.FromResult(HealthCheckResult.CreateInstance(func(x), type))));
    }

    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, string type, Func<IServiceResolver, Task<bool>> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(type, () => HealthCheckResult.CreateInstance(func(x), type)));
    }
    
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, Func<IServiceResolver, bool> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(() => Task.FromResult(HealthCheckResult.CreateInstance(func(x), "inline"))));
    }

    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, Func<IServiceResolver, Task<bool>> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(() => HealthCheckResult.CreateInstance(func(x), "inline")));
    }
}
