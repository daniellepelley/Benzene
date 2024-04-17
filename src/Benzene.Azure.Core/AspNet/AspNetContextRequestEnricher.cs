using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Request;
using Benzene.Core.Helper;
using Benzene.Http;

namespace Benzene.Azure.Core.AspNet;


public class AspNetContextRequestEnricher : IRequestEnricher<AspNetContext>
{
    private readonly IMessageHeadersMapper<AspNetContext> _headersToBodyMapper = new AspNetHeadersToBodyMapper();
    private readonly IRouteFinder _routeFinder;

    public AspNetContextRequestEnricher(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    public IDictionary<string, object> Enrich<TRequest>(TRequest request, AspNetContext context)
    {
        var route = _routeFinder.Find(context.HttpRequest.Method, context.HttpRequest.Path);

        if (route == null)
        {
            return new Dictionary<string, object>();
        }

        var dictionary = new Dictionary<string, object>();

        DictionaryUtils.MapOnto(dictionary, route.Parameters);
        DictionaryUtils.MapOnto(dictionary, _headersToBodyMapper.GetHeaders(context));
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
