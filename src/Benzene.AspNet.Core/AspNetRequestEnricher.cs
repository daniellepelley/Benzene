using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Core.Helper;
using Benzene.Http;
using Benzene.Http.Routing;

namespace Benzene.AspNet.Core;

public class AspNetRequestEnricher : IRequestEnricher<AspNetContext>
{
    private readonly IRouteFinder _routeFinder;
    private readonly AspNetHeadersToBodyMapper _headersToBodyMapper;

    public AspNetRequestEnricher(IRouteFinder routeFinder, IHttpHeaderMappings httpHeaderMappings)
    {
        _headersToBodyMapper = new AspNetHeadersToBodyMapper(httpHeaderMappings);
        _routeFinder = routeFinder;
    }

    public IDictionary<string, object> Enrich<TRequest>(TRequest request, AspNetContext context)
    {
        var route = _routeFinder.Find(context.HttpContext.Request.Method, context.HttpContext.Request.Path);
        var dictionary = new Dictionary<string, object>();

        if (route == null)
        {
            return dictionary;
        }

        DictionaryUtils.MapOnto(dictionary, context.HttpContext.Request.Query?.ToDictionary(x => x.Key, x => x.Value.First()));
        DictionaryUtils.MapOnto(dictionary, _headersToBodyMapper.GetHeaders(context));
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