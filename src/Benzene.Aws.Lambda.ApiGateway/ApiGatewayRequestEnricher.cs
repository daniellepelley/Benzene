using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Core.Helper;
using Benzene.Http;
using Benzene.Http.Routing;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Enriches a message request with values extracted from the API Gateway request — query string
/// parameters, path parameters, and mapped headers.
/// </summary>
public class ApiGatewayRequestEnricher : IRequestEnricher<ApiGatewayContext>
{
    private readonly IRouteFinder _routeFinder;
    private readonly IHttpHeaderMappings _httpHeaderMappings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayRequestEnricher"/> class.
    /// </summary>
    /// <param name="routeFinder">The route finder used to match the request to a registered route and its path parameters.</param>
    /// <param name="httpHeaderMappings">The header mappings used to select which headers are added to the enrichment dictionary.</param>
    public ApiGatewayRequestEnricher(IRouteFinder routeFinder, IHttpHeaderMappings httpHeaderMappings)
    {
        _httpHeaderMappings = httpHeaderMappings;
        _routeFinder = routeFinder;
    }

    /// <summary>
    /// Builds a dictionary of enrichment values from the API Gateway request's query string,
    /// path parameters, and mapped headers.
    /// </summary>
    /// <typeparam name="TRequest">The request type being enriched.</typeparam>
    /// <param name="request">The request being enriched.</param>
    /// <param name="context">The API Gateway context to extract enrichment values from.</param>
    /// <returns>A dictionary of enrichment values, keyed by property name. Empty if no route matches.</returns>
    public IDictionary<string, object> Enrich<TRequest>(TRequest request, ApiGatewayContext context)
    {
        var route = _routeFinder.Find(context.ApiGatewayProxyRequest.HttpMethod, context.ApiGatewayProxyRequest.Path);

        if (route == null)
        {
            return new Dictionary<string, object>();
        }

        var dictionary = new Dictionary<string, object>();

        DictionaryUtils.MapOnto(dictionary, context.ApiGatewayProxyRequest.QueryStringParameters);
        DictionaryUtils.MapOnto(dictionary, context.ApiGatewayProxyRequest.PathParameters);

        DictionaryUtils.MapOnto(dictionary, DictionaryUtils.FilterAndReplace(context.ApiGatewayProxyRequest.Headers, _httpHeaderMappings.GetMappings()));
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
