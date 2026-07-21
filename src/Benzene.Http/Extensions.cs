using Benzene.Abstractions.DI;
using Benzene.Http.Routing;

namespace Benzene.Http;

/// <summary>
/// Provides extension methods for configuring HTTP services and utilities in Benzene.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers HTTP message handler services, routing, and related infrastructure in the service container.
    /// </summary>
    /// <param name="services">The Benzene service container.</param>
    /// <returns>The service container for method chaining.</returns>
    /// <remarks>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><description>Endpoint finders: Reflection, List, Dependency, and Composite finders with caching</description></item>
    /// <item><description>Route finder for matching HTTP requests to endpoints</description></item>
    /// <item><description>HTTP status code mapper with default REST conventions</description></item>
    /// <item><description>HTTP header mappings with default empty mappings</description></item>
    /// </list>
    /// The composite endpoint finder combines reflection-based, list-based, and dependency-based
    /// discovery strategies, with caching applied to the reflection finder for performance.
    /// </remarks>
    public static IBenzeneServiceContainer AddHttpMessageHandlers(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<ReflectionHttpEndpointFinder>();
        services.TryAddSingleton<ListHttpEndpointFinder>();
        services.TryAddSingleton<DependencyHttpEndpointFinder>();
        services.TryAddSingleton<IListHttpEndpointFinder, ListHttpEndpointFinder>();
        // The UnroutedHttpEndpointCheck runs first: it contributes no endpoints but throws when a
        // scanned handler carries [HttpEndpoint] without [Message] (and no explicit registration),
        // turning the silently-missing-route failure mode into a clear error when routes are built.
        services.TryAddSingleton<IHttpEndpointFinder>(x =>
            new CompositeHttpEndpointFinder(
            new UnroutedHttpEndpointCheck(
            x.GetServices<Core.MessageHandlers.MessageHandlerCandidateTypes>(),
            x.GetService<Abstractions.MessageHandlers.IMessageHandlersFinder>()),
            new CacheHttpEndpointFinder(
            x.GetService<ReflectionHttpEndpointFinder>()),
            x.GetService<ListHttpEndpointFinder>(),
            x.GetService<DependencyHttpEndpointFinder>()));
        // The route table is compiled once in the singleton RouteFinder; a scoped MemoizingRouteFinder
        // wraps it so the topic getter, version getter, and enricher that each resolve the route for
        // one request share a single match instead of re-running it 2-3x per request.
        services.TryAddSingleton<RouteFinder>();
        services.TryAddScoped<IRouteFinder>(x => new MemoizingRouteFinder(x.GetService<RouteFinder>()));
        
        services.TryAddScoped<IHttpStatusCodeMapper, DefaultHttpStatusCodeMapper>();
        services.TryAddScoped<IHttpHeaderMappings, DefaultHttpHeaderMappings>();
        return services;
    }
    
    /// <summary>
    /// Converts an HTTP request to lowercase for case-insensitive processing.
    /// </summary>
    /// <param name="source">The HTTP request to convert.</param>
    /// <returns>
    /// A new <see cref="HttpRequest"/> with the path, method, and header names converted to lowercase.
    /// </returns>
    /// <remarks>
    /// This method is useful for normalizing HTTP requests for case-insensitive comparison,
    /// particularly for routing and header matching. Header values are preserved as-is; only
    /// header names are converted to lowercase.
    /// </remarks>
    public static HttpRequest AsLowerCase(this HttpRequest source)
    {
        return new HttpRequest
        {
            Path = source.Path.ToLowerInvariant(),
            Method = source.Method.ToLowerInvariant(),
            Headers = source.Headers.ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Value)
        };
    }
}
