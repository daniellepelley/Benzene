﻿using Benzene.Abstractions.Request;
using Benzene.Core.Helper;
using Benzene.Http;

namespace Benzene.SelfHost.Http;


public class HttpRequestEnricher : IRequestEnricher<HttpContext>
{
    private readonly IRouteFinder _routeFinder;
    private readonly IHttpHeaderMappings _httpHeaderMappings;

    public HttpRequestEnricher(IRouteFinder routeFinder, IHttpHeaderMappings httpHeaderMappings)
    {
        _httpHeaderMappings = httpHeaderMappings;
        _routeFinder = routeFinder;
    }

    public IDictionary<string, object> Enrich<TRequest>(TRequest request, HttpContext context)
    {
        var route = _routeFinder.Find(context.Request.Method, context.Request.Path);

        if (route == null)
        {
            return new Dictionary<string, object>();
        }

        var dictionary = new Dictionary<string, object>();

        DictionaryUtils.MapOnto(dictionary, context.Request.QueryParameters);

        DictionaryUtils.MapOnto(dictionary, DictionaryUtils.FilterAndReplace(context.Request.Headers, _httpHeaderMappings.GetMappings()));
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
