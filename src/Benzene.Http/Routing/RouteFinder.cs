using System;
using System.Linq;

namespace Benzene.Http.Routing;

/// <summary>
/// Provides the default implementation of <see cref="IRouteFinder"/> that matches HTTP requests to configured endpoints.
/// </summary>
/// <remarks>
/// This class uses an <see cref="IHttpEndpointFinder"/> to discover all available endpoints at construction
/// time, then matches incoming requests against these endpoints using HTTP method and URL path comparison.
/// The matching is case-insensitive and supports route parameters (e.g., "/users/{id}").
/// <para>
/// Each route's method is lower-cased and its path pattern is compiled to a <see cref="CompiledRoutePath"/>
/// once, at construction; per request only the incoming path is split (once, not once per route), so the
/// hot path does no per-route pattern re-splitting or regex work.
/// </para>
/// </remarks>
public class RouteFinder : IRouteFinder
{
    private readonly CompiledRoute[] _routes;

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteFinder"/> class.
    /// </summary>
    /// <param name="httpEndpointFinder">The endpoint finder used to discover available HTTP endpoints.</param>
    public RouteFinder(IHttpEndpointFinder httpEndpointFinder)
    {
        // Try more-specific routes (fewer {parameter} segments) first, so a literal route like
        // /users/me is matched ahead of /users/{id} regardless of discovery order. Find returns the
        // first match, and discovery order is effectively arbitrary (reflection order), so without
        // this a parameter route could silently shadow a literal one and make it unreachable.
        // OrderBy is stable, so routes of equal specificity keep their original relative order.
        _routes = httpEndpointFinder.FindDefinitions()
            .OrderBy(CountParameterSegments)
            .Select(route => new CompiledRoute(route.Method.ToLowerInvariant(), new CompiledRoutePath(route.Path), route.Topic))
            .ToArray();
    }

    private static int CountParameterSegments(IHttpEndpointDefinition route)
    {
        return route.Path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Count(segment => segment.Contains('{'));
    }

    /// <summary>
    /// Finds an HTTP route that matches the specified HTTP method and path.
    /// </summary>
    /// <param name="method">The HTTP method (GET, POST, PUT, DELETE, etc.) to match.</param>
    /// <param name="path">The URL path to match against registered route patterns.</param>
    /// <returns>
    /// An <see cref="HttpTopicRoute"/> containing the matched route and extracted parameters,
    /// or <c>null</c> if no matching route is found.
    /// </returns>
    public HttpTopicRoute? Find(string method, string path)
    {
        var lowerMethod = method.ToLowerInvariant();
        var pathParts = UrlMatcher.SplitPath(path);

        foreach (var route in _routes)
        {
            if (route.Method != lowerMethod)
            {
                continue;
            }

            var parameters = route.Path.Match(pathParts);

            if (parameters != null)
            {
                return new HttpTopicRoute(route.Topic, parameters);
            }
        }

        return null;
    }

    private sealed class CompiledRoute
    {
        public CompiledRoute(string method, CompiledRoutePath path, string topic)
        {
            Method = method;
            Path = path;
            Topic = topic;
        }

        public string Method { get; }
        public CompiledRoutePath Path { get; }
        public string Topic { get; }
    }
}
