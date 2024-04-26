using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Response;using Benzene.Abstractions.Results;
using Benzene.Core.Results;
using Benzene.Http.Cors;
using Benzene.Http.Routing;

public class HttpRequestAdapter
{
    
}

public interface IHttpRequestAdapter<TContext> where TContext : IHttpContext
{
    HttpRequest2 Map(TContext context);
}

public interface IHttpContext
{
    
}

public class HttpRequest2
{
    public string Method { get; set; }
    public string Path { get; set; }
    public IDictionary<string, string> Headers { get; set; }
}

public class CorsMiddleware<TContext> : IMiddleware<TContext> where TContext : IHasMessageResult, IHttpContext
{
    private readonly CorsSettings _corsSettings;
    private readonly IHttpEndpointFinder _httpEndpointFinder;
    private readonly UrlMatcher _urlMatcher;
    private readonly IHttpRequestAdapter<TContext> _httpRequestAdapter;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;
    public string Name => "Cors";

    public CorsMiddleware(CorsSettings corsSettings, IHttpEndpointFinder httpEndpointFinder,
        IHttpRequestAdapter<TContext> httpRequestAdapter, IBenzeneResponseAdapter<TContext> responseAdapter)
    {
        _responseAdapter = responseAdapter;
        _httpRequestAdapter = httpRequestAdapter;
        _httpEndpointFinder = httpEndpointFinder;
        _corsSettings = corsSettings;
        _urlMatcher = new UrlMatcher();
    }

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var httpRequest = _httpRequestAdapter.Map(context);
        if (httpRequest.Method.ToLowerInvariant() != "options")
        {
            await next();
        }

        AddCorsHeaders(context, httpRequest);
    }

    private void AddCorsHeaders(TContext context, HttpRequest2 httpRequest)
    {
        if (httpRequest.Headers.Any(header => header.Key.ToLowerInvariant() == "origin"))
        {
            var methods = FindMethods(httpRequest.Path);

            if (!methods.Any())
            {
                return;
            }

            var origin = httpRequest.Headers["origin"];

            if (_corsSettings.AllowedDomains.Contains(origin))
            {
                _responseAdapter.SetResponseHeader(context, "Access-Control-Allow-Origin", origin);
                _responseAdapter.SetResponseHeader(context, "Access-Control-Allow-Headers",
                    string.Join(",", _corsSettings.AllowedHeaders));
                _responseAdapter.SetResponseHeader(context, "Access-Control-Allow-Methods",
                    "OPTIONS," + string.Join(",", methods));
            }

            if (context.MessageResult == null)
            {
                context.MessageResult = new MessageResult("cors", null, "Ok", true, null, null);
            }
        }
    }

    private string[] FindMethods(string path)
    {
        var output = new List<string>();
        var routes = _httpEndpointFinder.FindDefinitions();
        foreach (var route in routes)
        {
            var parameters = _urlMatcher.MatchUrl(path, route.Path);

            if (parameters != null)
            {
                output.Add(route.Method.ToUpperInvariant());
            }
        }

        return output.ToArray();
    }
}
