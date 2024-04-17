using Benzene.Abstractions.DI;
using Benzene.Core.DI;

namespace Benzene.Http;

public static class Extensions
{
    public static IBenzeneServiceContainer AddHttpMessageHandlers(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<HttpEndpointFinder>();
        services.TryAddScoped<IHttpEndpointFinder>(x => new CacheHttpEndpointFinder(x.GetService<HttpEndpointFinder>()));
        services.TryAddScoped<IHttpStatusCodeMapper, DefaultHttpStatusCodeMapper>();
        services.TryAddScoped<IHttpHeaderMappings, DefaultHttpHeaderMappings>();
        services.TryAddScoped<IRouteFinder, RouteFinder>();
        return services;
    }
}
