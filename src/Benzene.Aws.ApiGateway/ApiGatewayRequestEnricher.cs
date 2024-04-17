using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Request;
using Benzene.Core.Helper;
using Benzene.Http;

namespace Benzene.Aws.ApiGateway;


public class ApiGatewayRequestEnricher : IRequestEnricher<ApiGatewayContext>
{
    private readonly IRouteFinder _routeFinder;
    private readonly IHttpHeaderMappings _httpHeaderMappings;

    public ApiGatewayRequestEnricher(IRouteFinder routeFinder, IHttpHeaderMappings httpHeaderMappings)
    {
        _httpHeaderMappings = httpHeaderMappings;
        _routeFinder = routeFinder;
    }

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
