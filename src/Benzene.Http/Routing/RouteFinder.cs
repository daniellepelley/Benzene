namespace Benzene.Http.Routing;

/// <summary>
/// Provides the default implementation of <see cref="IRouteFinder"/> that matches HTTP requests to configured endpoints.
/// </summary>
/// <remarks>
/// This class uses an <see cref="IHttpEndpointFinder"/> to discover all available endpoints at construction
/// time, then matches incoming requests against these endpoints using HTTP method and URL path comparison.
/// The matching is case-insensitive and supports route parameters (e.g., "/users/{id}").
/// </remarks>
public class RouteFinder : IRouteFinder
{
    private readonly IHttpEndpointDefinition[] _routes;
    private readonly UrlMatcher _urlMatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteFinder"/> class.
    /// </summary>
    /// <param name="httpEndpointFinder">The endpoint finder used to discover available HTTP endpoints.</param>
    public RouteFinder(IHttpEndpointFinder httpEndpointFinder)
    {
        _routes = httpEndpointFinder.FindDefinitions();
        _urlMatcher = new UrlMatcher();
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
        foreach (var route in _routes)
        {
            if (route.Method.ToLowerInvariant() != lowerMethod)
            {
                continue;
            }

            var parameters = _urlMatcher.MatchUrl(path, route.Path);

            if (parameters != null)
            {
                return new HttpTopicRoute(route.Topic, parameters);
            }
        }

        return null;
    }
}

