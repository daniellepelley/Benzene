using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Http.Routing;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Http.Cors;

/// <summary>
/// Middleware that handles Cross-Origin Resource Sharing (CORS) for HTTP requests.
/// </summary>
/// <typeparam name="TContext">The HTTP context type.</typeparam>
/// <remarks>
/// This middleware processes CORS preflight (OPTIONS) requests and adds appropriate CORS headers
/// to responses. It validates the Origin header against configured allowed domains and determines
/// which HTTP methods are available for the requested path by examining registered endpoints.
/// CORS middleware should be added early in the pipeline to ensure it processes all requests.
/// </remarks>
public class CorsMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext
{
    private readonly CorsSettings _corsSettings;
    private readonly IHttpEndpointFinder _httpEndpointFinder;
    private readonly UrlMatcher _urlMatcher;
    private readonly IHttpRequestAdapter<TContext> _httpRequestAdapter;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;
    private CorsOriginChecker _corsOriginChecker;
    private IMessageHandlerResultSetter<TContext> _messageHandlerResultSetter;

    /// <summary>
    /// Gets the name of the middleware.
    /// </summary>
    public string Name => "Cors";

    /// <summary>
    /// Initializes a new instance of the <see cref="CorsMiddleware{TContext}"/> class.
    /// </summary>
    /// <param name="corsSettings">The CORS configuration settings.</param>
    /// <param name="httpEndpointFinder">The endpoint finder used to discover available HTTP methods for a path.</param>
    /// <param name="httpRequestAdapter">The adapter used to convert the context to an HTTP request.</param>
    /// <param name="responseAdapter">The adapter used to set response headers.</param>
    /// <param name="messageHandlerResultSetter">The result setter for handling OPTIONS requests.</param>
    public CorsMiddleware(CorsSettings corsSettings, IHttpEndpointFinder httpEndpointFinder,
        IHttpRequestAdapter<TContext> httpRequestAdapter, IBenzeneResponseAdapter<TContext> responseAdapter, IMessageHandlerResultSetter<TContext> messageHandlerResultSetter)
    {
        _messageHandlerResultSetter = messageHandlerResultSetter;
        _responseAdapter = responseAdapter;
        _httpRequestAdapter = httpRequestAdapter;
        _httpEndpointFinder = httpEndpointFinder;
        _corsSettings = corsSettings;
        _urlMatcher = new UrlMatcher();
        _corsOriginChecker = new CorsOriginChecker();
    }

    /// <summary>
    /// Handles the HTTP request, processing CORS headers and preflight requests.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var httpRequest = _httpRequestAdapter.Map(context).AsLowerCase();
        if (httpRequest.Method.ToLowerInvariant() != "options")
        {
            await next();
        }

        await AddCorsHeaders(context, httpRequest);
    }

    private async Task AddCorsHeaders(TContext context, HttpRequest httpRequest)
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
                await _messageHandlerResultSetter.SetResultAsync(context, new MessageHandlerResult(new Topic("cors"), MessageHandlerDefinition.CreateInstance("cors", typeof(Void), typeof(Void)), BenzeneResult.Ok()));
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

/// <summary>
/// Validates CORS origin headers against a list of allowed domains.
/// </summary>
/// <remarks>
/// This class checks whether the Origin header in an HTTP request matches one of the
/// configured allowed domains. Domain matching is performed on the host portion of the
/// URL, ignoring protocols and paths, and is case-insensitive.
/// </remarks>
public class CorsOriginChecker
{
    /// <summary>
    /// Matches the request's Origin header against a list of allowed domains.
    /// </summary>
    /// <param name="allowedDomains">The list of allowed domains (can be full URLs or hostnames).</param>
    /// <param name="httpRequest">The HTTP request containing the Origin header.</param>
    /// <returns>
    /// The original origin value if it matches an allowed domain, or <c>null</c> if the origin
    /// is not present, invalid, or not in the allowed list.
    /// </returns>
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