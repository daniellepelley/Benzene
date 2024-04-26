using Benzene.Abstractions.Request;
using Benzene.Core.Helper;
using Benzene.Http;
using Benzene.Http.Routing;

namespace Benzene.SelfHost.Http;

public class HttpListenerRequestEnricher : IRequestEnricher<SelfHostHttpContext>
{
    private readonly IRouteFinder _routeFinder;
    private readonly IHttpHeaderMappings _httpHeaderMappings;

    public HttpListenerRequestEnricher(IRouteFinder routeFinder, IHttpHeaderMappings httpHeaderMappings)
    {
        _httpHeaderMappings = httpHeaderMappings;
        _routeFinder = routeFinder;
    }

    public IDictionary<string, object> Enrich<TRequest>(TRequest request, SelfHostHttpContext context)
    {
        var route = _routeFinder.Find(context.HttpListenerContext.Request.HttpMethod, context.HttpListenerContext.Request.RawUrl);

        if (route == null)
        {
            return new Dictionary<string, object>();
        }

        var dictionary = new Dictionary<string, object>();

        DictionaryUtils.MapOnto(dictionary, context.HttpListenerContext.Request.QueryString.ToDictionary());

        DictionaryUtils.MapOnto(dictionary, DictionaryUtils.FilterAndReplace(context.HttpListenerContext.Request.Headers.ToDictionary(), _httpHeaderMappings.GetMappings()));
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
