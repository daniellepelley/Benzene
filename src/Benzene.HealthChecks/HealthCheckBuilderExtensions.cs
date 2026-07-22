using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// Convenience overloads for registering a health check as an inline function against an
/// <see cref="IHealthCheckBuilder"/>, without having to write a dedicated <see cref="IHealthCheck"/>
/// class. Each overload builds an <see cref="InlineHealthCheck"/> under the hood.
/// </summary>
public static class HealthCheckBuilderExtensions
{
    /// <summary>Registers a named inline health check that synchronously produces its result.</summary>
    /// <param name="source">The builder to register the check with.</param>
    /// <param name="type">The check's identifier, used as its key in the aggregated response.</param>
    /// <param name="func">Computes the result of the check.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, string type, Func<IServiceResolver, IHealthCheckResult> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(type, () => Task.FromResult(func(x))));
    }

    /// <summary>Registers a named inline health check that asynchronously produces its result.</summary>
    /// <param name="source">The builder to register the check with.</param>
    /// <param name="type">The check's identifier, used as its key in the aggregated response.</param>
    /// <param name="func">Computes the result of the check.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, string type, Func<IServiceResolver, Task<IHealthCheckResult>> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(type, () => func(x)));
    }

    /// <summary>Registers an unnamed inline health check that synchronously produces its result.</summary>
    /// <param name="source">The builder to register the check with.</param>
    /// <param name="func">Computes the result of the check.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, Func<IServiceResolver, IHealthCheckResult> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(() => Task.FromResult(func(x))));
    }

    /// <summary>Registers an unnamed inline health check that asynchronously produces its result.</summary>
    /// <param name="source">The builder to register the check with.</param>
    /// <param name="func">Computes the result of the check.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, Func<IServiceResolver, Task<IHealthCheckResult>> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(() => func(x)));
    }

    /// <summary>Registers a named inline health check that synchronously reports success/failure as a <see cref="bool"/>.</summary>
    /// <param name="source">The builder to register the check with.</param>
    /// <param name="type">The check's identifier, used as its key in the aggregated response.</param>
    /// <param name="func">Returns <c>true</c> if the check passed, <c>false</c> otherwise.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, string type, Func<IServiceResolver, bool> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(type, () => Task.FromResult(HealthCheckResult.CreateInstance(func(x), type))));
    }

    /// <summary>Registers a named inline health check that asynchronously reports success/failure as a <see cref="bool"/>.</summary>
    /// <param name="source">The builder to register the check with.</param>
    /// <param name="type">The check's identifier, used as its key in the aggregated response.</param>
    /// <param name="func">Returns <c>true</c> if the check passed, <c>false</c> otherwise.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, string type, Func<IServiceResolver, Task<bool>> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(type, () => HealthCheckResult.CreateInstance(func(x), type)));
    }

    /// <summary>Registers an unnamed (type <c>"inline"</c>) inline health check that synchronously reports success/failure as a <see cref="bool"/>.</summary>
    /// <param name="source">The builder to register the check with.</param>
    /// <param name="func">Returns <c>true</c> if the check passed, <c>false</c> otherwise.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, Func<IServiceResolver, bool> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(() => Task.FromResult(HealthCheckResult.CreateInstance(func(x), "inline"))));
    }

    /// <summary>Registers an unnamed (type <c>"inline"</c>) inline health check that asynchronously reports success/failure as a <see cref="bool"/>.</summary>
    /// <param name="source">The builder to register the check with.</param>
    /// <param name="func">Returns <c>true</c> if the check passed, <c>false</c> otherwise.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, Func<IServiceResolver, Task<bool>> func)
    {
        return source.AddHealthCheck(x => new InlineHealthCheck(() => HealthCheckResult.CreateInstance(func(x), "inline")));
    }

    /// <summary>
    /// Registers a health check for a named third-party dependency from a non-destructive probe delegate -
    /// the low-ceremony "bring your own check" path. The <paramref name="probe"/> returning normally is
    /// <see cref="HealthCheckStatus.Ok"/>; throwing is <see cref="HealthCheckStatus.Failed"/> (its
    /// exception <em>type</em> is recorded under <c>Data["Error"]</c> - never the message, which may carry
    /// secrets). The result carries a <see cref="HealthCheckDependency"/> of <paramref name="kind"/> +
    /// <paramref name="name"/>, so the dependency surfaces in the mesh like the shipped client checks do.
    /// Timeout and exception isolation are applied by the aggregating processor, so the probe may safely
    /// throw or block. Keep it read-only and cheap (it will be polled).
    /// </summary>
    /// <param name="source">The builder to register the check with.</param>
    /// <param name="kind">The dependency category (e.g. <c>"Database"</c>, <c>"Http"</c>, <c>"Queue"</c>), used as the <see cref="HealthCheckDependency.Kind"/>.</param>
    /// <param name="name">The specific resource identifier (e.g. <c>"orders-db"</c>) - never a connection string or secret. Used as both the check's <see cref="IHealthCheck.Type"/> and the <see cref="HealthCheckDependency.Name"/>.</param>
    /// <param name="probe">A non-destructive reachability probe: returns to report healthy, throws to report unhealthy.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, string kind, string name, Func<Task> probe)
    {
        var dependencies = new[] { new HealthCheckDependency(kind, name) };
        return source.AddHealthCheck(_ => new InlineHealthCheck(name, async () =>
        {
            try
            {
                await probe();
                return HealthCheckResult.CreateInstance(true, name, new Dictionary<string, object>(), dependencies);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.CreateInstance(false, name,
                    new Dictionary<string, object> { { "Error", ex.GetType().Name } }, dependencies);
            }
        }));
    }

    /// <summary>
    /// As <see cref="AddHealthCheck(IHealthCheckBuilder, string, string, Func{Task})"/>, but the probe
    /// reports success as a <see cref="bool"/> (<c>true</c> = <see cref="HealthCheckStatus.Ok"/>,
    /// <c>false</c> = <see cref="HealthCheckStatus.Failed"/>) instead of by throwing - for a probe with no
    /// natural exception (e.g. an HTTP status-code check). A thrown exception is still reported as failed.
    /// </summary>
    /// <param name="source">The builder to register the check with.</param>
    /// <param name="kind">The dependency category, used as the <see cref="HealthCheckDependency.Kind"/>.</param>
    /// <param name="name">The specific resource identifier, used as the check's <see cref="IHealthCheck.Type"/> and the <see cref="HealthCheckDependency.Name"/>.</param>
    /// <param name="probe">A non-destructive reachability probe returning <c>true</c> if healthy.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, string kind, string name, Func<Task<bool>> probe)
    {
        var dependencies = new[] { new HealthCheckDependency(kind, name) };
        return source.AddHealthCheck(_ => new InlineHealthCheck(name, async () =>
        {
            try
            {
                var ok = await probe();
                return HealthCheckResult.CreateInstance(ok, name, new Dictionary<string, object>(), dependencies);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.CreateInstance(false, name,
                    new Dictionary<string, object> { { "Error", ex.GetType().Name } }, dependencies);
            }
        }));
    }
}
