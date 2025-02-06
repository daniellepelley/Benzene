using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers.Filters;

public static class DependencyExtensions
{
    public static IMessageRouterBuilder UseFilters(this IMessageRouterBuilder builder, params Assembly[] assemblies)
    {
        builder.Register(x => x.AddFilters(assemblies));
        return builder.Add(new FiltersMiddlewareBuilder());
    }
    
    public static IMessageRouterBuilder UseFilters(this IMessageRouterBuilder builder, Type[] types)
    {
        builder.Register(x => x.AddFilters(types));
        return builder.Add(new FiltersMiddlewareBuilder());
    }

    public static IBenzeneServiceContainer AddFilters(this IBenzeneServiceContainer services, params Assembly[] assemblies)
    {
        return services.AddFilters(Utils.GetAllTypes(assemblies).ToArray());
    }

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
