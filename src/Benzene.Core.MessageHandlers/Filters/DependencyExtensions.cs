using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers.Filters;

/// <summary>
/// DI and pipeline-builder extension methods for registering <see cref="IFilter{T}"/> implementations
/// and enabling filter execution for handler dispatch.
/// </summary>
public static class DependencyExtensions
{
    /// <summary>
    /// Registers every <see cref="IFilter{T}"/> found by reflection over the given assemblies, and adds
    /// a <see cref="FiltersMiddlewareBuilder"/> so filters run for every handler.
    /// </summary>
    /// <param name="builder">The router builder to configure.</param>
    /// <param name="assemblies">The assemblies to scan for <see cref="IFilter{T}"/> implementations.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMessageRouterBuilder UseFilters(this IMessageRouterBuilder builder, params Assembly[] assemblies)
    {
        builder.Register(x => x.AddFilters(assemblies));
        return builder.Add(new FiltersMiddlewareBuilder());
    }

    /// <summary>
    /// Registers every <see cref="IFilter{T}"/> found among the given candidate types, and adds a
    /// <see cref="FiltersMiddlewareBuilder"/> so filters run for every handler.
    /// </summary>
    /// <param name="builder">The router builder to configure.</param>
    /// <param name="types">The candidate types to inspect for <see cref="IFilter{T}"/> implementations.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMessageRouterBuilder UseFilters(this IMessageRouterBuilder builder, Type[] types)
    {
        builder.Register(x => x.AddFilters(types));
        return builder.Add(new FiltersMiddlewareBuilder());
    }

    /// <summary>
    /// Registers every <see cref="IFilter{T}"/> found by reflection over the given assemblies as a singleton, keyed by its <c>IFilter&lt;T&gt;</c> interface.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <param name="assemblies">The assemblies to scan for <see cref="IFilter{T}"/> implementations.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddFilters(this IBenzeneServiceContainer services, params Assembly[] assemblies)
    {
        return services.AddFilters(Utils.GetAllTypes(assemblies).ToArray());
    }

    /// <summary>
    /// Registers every non-abstract <see cref="IFilter{T}"/> implementation among the given candidate
    /// types as a singleton, keyed by its <c>IFilter&lt;T&gt;</c> interface.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <param name="types">The candidate types to inspect for <see cref="IFilter{T}"/> implementations.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddFilters(this IBenzeneServiceContainer services, Type[] types)
    {
        var filterTypes = types
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFilter<>)) && !t.IsAbstract)
            .ToArray();


        foreach (var filterType in filterTypes)
        {
            services.AddSingleton(filterType.GetInterface("IFilter`1"), filterType);
        }

        return services;
    }
}
