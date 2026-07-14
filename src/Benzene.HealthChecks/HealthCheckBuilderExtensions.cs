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
}
