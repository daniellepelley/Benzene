namespace Benzene.Http;

public class RouteFinder : IRouteFinder
{
    private readonly IHttpEndpointDefinition[] _routes;
    private readonly UrlMatcher _urlMatcher;

    public RouteFinder(IHttpEndpointFinder httpEndpointFinder)
    {
        _routes = httpEndpointFinder.FindDefinitions();
        _urlMatcher = new UrlMatcher();
    }

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

