using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Http.Routing;
using Benzene.Results;
using Void = Benzene.Results.Void;

namespace Benzene.Http.Cors;

public class CorsMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext
{
    private readonly CorsSettings _corsSettings;
    private readonly IHttpEndpointFinder _httpEndpointFinder;
    private readonly UrlMatcher _urlMatcher;
    private readonly IHttpRequestAdapter<TContext> _httpRequestAdapter;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;
    private CorsOriginChecker _corsOriginChecker;
    private IResultSetter<TContext> _resultSetter;
    public string Name => "Cors";

    public CorsMiddleware(CorsSettings corsSettings, IHttpEndpointFinder httpEndpointFinder,
        IHttpRequestAdapter<TContext> httpRequestAdapter, IBenzeneResponseAdapter<TContext> responseAdapter, IResultSetter<TContext> resultSetter)
    {
        _resultSetter = resultSetter;
        _responseAdapter = responseAdapter;
        _httpRequestAdapter = httpRequestAdapter;
        _httpEndpointFinder = httpEndpointFinder;
        _corsSettings = corsSettings;
        _urlMatcher = new UrlMatcher();
        _corsOriginChecker = new CorsOriginChecker();
    }

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var httpRequest = _httpRequestAdapter.Map(context).AsLowerCase();
        if (httpRequest.Method.ToLowerInvariant() != "options")
        {
            await next();
        }

        AddCorsHeaders(context, httpRequest);
    }

    private void AddCorsHeaders(TContext context, HttpRequest httpRequest)
    {
        if (httpRequest.Headers.Any(header => header.Key.ToLowerInvariant() == "origin"))
        {
            var methods = FindMethods(httpRequest.Path);

            if (!methods.Any())
            {
                return;
            }

            var origin = _corsOriginChecker.MatchOrigin(_corsSettings.AllowedDomains, httpRequest);

            if (origin != null)
            {
                _responseAdapter.SetResponseHeader(context, "Access-Control-Allow-Origin", origin);
                _responseAdapter.SetResponseHeader(context, "Access-Control-Allow-Headers",
                    string.Join(",", _corsSettings.AllowedHeaders));
                _responseAdapter.SetResponseHeader(context, "Access-Control-Allow-Methods",
                    "OPTIONS," + string.Join(",", methods));
            }

            if (httpRequest.Method == "options")
            {
                _resultSetter.SetResultAsync(context, new MessageHandlerResult(new Topic("cors"), MessageHandlerDefinition.CreateInstance("cors", typeof(Void), typeof(Void)), BenzeneResult.Ok()));
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

public class CorsOriginChecker
{
    public string? MatchOrigin(string[] allowedDomains, HttpRequest httpRequest)
    {
        if (httpRequest.Headers == null ||
            !httpRequest.Headers.ContainsKey("origin"))
        {
            return null;
        }
        
        var origin = httpRequest.Headers["origin"];
        if (origin == null)
        {
            return null;
        }
        
        if (allowedDomains
            .Any(x => GetDomain(x) == GetDomain(origin)))
        {
            return origin;
        }

        return null;
    }

    private static string? GetDomain(string url)
    {
        try
        {
            return new Uri(url).Host.ToLowerInvariant();
        }
        catch
        {
            return url.Replace("/", "");
        }
    }
}