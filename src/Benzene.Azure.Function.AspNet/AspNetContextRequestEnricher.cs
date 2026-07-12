using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.Helper;
using Benzene.Http.Routing;

namespace Benzene.Azure.Function.AspNet;

/// <summary>
/// Enriches a message handler's request with values from the HTTP query string, mapped headers, and
/// route parameters matched against the request's method and path.
/// </summary>
public class AspNetContextRequestEnricher : IRequestEnricher<AspNetContext>
{
    private readonly IMessageHeadersGetter<AspNetContext> _headersToBodyGetter = new AspNetHeadersToBodyGetter();
    private readonly IRouteFinder _routeFinder;

    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetContextRequestEnricher"/> class.
    /// </summary>
    /// <param name="routeFinder">The route finder used to match the request's method and path to a route.</param>
    public AspNetContextRequestEnricher(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    /// <summary>
    /// Builds a dictionary of values to enrich the request with, combining the query string, headers
    /// mapped by <see cref="AspNetHeadersToBodyGetter"/>, and matched route parameters. Returns an empty
    /// dictionary if no route matches.
    /// </summary>
    /// <typeparam name="TRequest">The message handler's request type.</typeparam>
    /// <param name="request">The request being enriched (unused; enrichment is derived entirely from the context).</param>
    /// <param name="context">The HTTP context to enrich from.</param>
    /// <returns>The dictionary of enrichment values.</returns>
    public IDictionary<string, object> Enrich<TRequest>(TRequest request, AspNetContext context)
    {
        var route = _routeFinder.Find(context.HttpRequest.Method, context.HttpRequest.Path);

        if (route == null)
        {
            return new Dictionary<string, object>();
        }

        var dictionary = new Dictionary<string, object>();

        DictionaryUtils.MapOnto(dictionary, context.HttpRequest.Query?.ToDictionary(x => x.Key, x => x.Value.ToString()));
        DictionaryUtils.MapOnto(dictionary, _headersToBodyGetter.GetHeaders(context));
        DictionaryUtils.MapOnto(dictionary, CleanUp(route.Parameters));
        // DictionaryUtils.MapOnto(dictionary, DictionaryUtils.JsonToDictionary(context.ApiGatewayProxyRequest.Body));

        return dictionary;
    }

    private static IDictionary<string, object> CleanUp(IDictionary<string, object> source)
    {
        return source
            // .Where(x => !x.Value.ToString()!.StartsWith("{"))
            .ToDictionary(x => x.Key, x => x.Value);
    }
}
