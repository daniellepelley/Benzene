using Benzene.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Benzene.Test.Azure;

public static class HttpBuilderExtensions
{
    public static HttpRequest AsAspNetCoreHttpRequest(this HttpBuilder source)
    {
        var request = new TestHttpRequest
        {
            Method = source.Method,
            Path = source.Path, 
            Body = Utils.ObjectToStream(source.Message),
        };

        foreach (var header in source.Headers)
        {
            request.Headers.Add(header.Key, new StringValues(header.Value));
        }

        return request;
    }
}
