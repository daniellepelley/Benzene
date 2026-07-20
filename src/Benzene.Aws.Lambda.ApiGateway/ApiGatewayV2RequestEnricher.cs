using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Core.Helper;
using Benzene.Http;
using Benzene.Http.Routing;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Enriches a message request with values extracted from the API Gateway HTTP API v2 request — query
/// string parameters, path parameters, and mapped headers (cookies folded in).
/// </summary>
public class ApiGatewayV2RequestEnricher : IRequestEnricher<ApiGatewayV2Context>
{
    private readonly IRouteFinder _routeFinder;
    private readonly IHttpHeaderMappings _httpHeaderMappings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayV2RequestEnricher"/> class.
    /// </summary>
    /// <param name="routeFinder">The route finder used to match the request to a registered route and its path parameters.</param>
    /// <param name="httpHeaderMappings">The header mappings used to select which headers are added to the enrichment dictionary.</param>
    public ApiGatewayV2RequestEnricher(IRouteFinder routeFinder, IHttpHeaderMappings httpHeaderMappings)
    {
        _httpHeaderMappings = httpHeaderMappings;
        _routeFinder = routeFinder;
    }

    /// <summary>
    /// Builds a dictionary of enrichment values from the API Gateway v2 request's query string,
    /// path parameters, and mapped headers.
    /// </summary>
    /// <typeparam name="TRequest">The request type being enriched.</typeparam>
    /// <param name="request">The request being enriched.</param>
    /// <param name="context">The API Gateway v2 context to extract enrichment values from.</param>
    /// <returns>A dictionary of enrichment values, keyed by property name. Empty if no route matches.</returns>
    public IDictionary<string, object> Enrich<TRequest>(TRequest request, ApiGatewayV2Context context)
    {
        var route = _routeFinder.Find(context.Method, context.Path);

        if (route == null)
        {
            return new Dictionary<string, object>();
        }

        var dictionary = new Dictionary<string, object>();

        DictionaryUtils.MapOnto(dictionary, context.ApiGatewayProxyRequest.QueryStringParameters);
        DictionaryUtils.MapOnto(dictionary, context.ApiGatewayProxyRequest.PathParameters);

        DictionaryUtils.MapOnto(dictionary, DictionaryUtils.FilterAndReplace(context.CombinedHeaders(), _httpHeaderMappings.GetMappings()));
        DictionaryUtils.MapOnto(dictionary, CleanUp(route.Parameters));

        return dictionary;
    }

    private static IDictionary<string, object> CleanUp(IDictionary<string, object> source)
    {
        return source
            .Where(x => !x.Value.ToString()!.StartsWith("{"))
            .ToDictionary(x => x.Key, x => x.Value);
    }
}
