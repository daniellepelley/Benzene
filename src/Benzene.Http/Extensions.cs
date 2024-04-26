using Benzene.Abstractions.DI;
using Benzene.Core.DI;
using Benzene.Http.Routing;

namespace Benzene.Http;

public static class Extensions
{
    public static IBenzeneServiceContainer AddHttpMessageHandlers(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<HttpEndpointFinder>();
        services.TryAddSingleton<ListHttpEndpointFinder>();
        services.TryAddSingleton<DependencyHttpEndpointFinder>();
        services.TryAddSingleton<IListHttpEndpointFinder, ListHttpEndpointFinder>();
        services.TryAddSingleton<IHttpEndpointFinder>(x =>
            new CompositeHttpEndpointFinder(
            new CacheHttpEndpointFinder(
            x.GetService<HttpEndpointFinder>()),
            x.GetService<ListHttpEndpointFinder>(),
            x.GetService<DependencyHttpEndpointFinder>()));
        services.TryAddSingleton<IRouteFinder, RouteFinder>();
        
        services.TryAddScoped<IHttpStatusCodeMapper, DefaultHttpStatusCodeMapper>();
        services.TryAddScoped<IHttpHeaderMappings, DefaultHttpHeaderMappings>();
        return services;
    }
}
