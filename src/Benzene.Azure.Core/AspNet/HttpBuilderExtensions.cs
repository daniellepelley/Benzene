using System.Text;
using Benzene.Abstractions;
using Benzene.Test.Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Benzene.Azure.Core.AspNet;

public static class HttpBuilderExtensions
{
    public static HttpRequest AsAspNetCoreHttpRequest(this IHttpBuilder source)
    {
        var request = new TestHttpRequest
        {
            Method = source.Method,
            Path = source.Path, 
            Body = ObjectToStream(source.Message),
        };

        foreach (var header in source.Headers)
        {
            request.Headers.Add(header.Key, new StringValues(header.Value));
        }

        return request;
    }

    private static MemoryStream ObjectToStream(object obj)
    {
        var byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        return new MemoryStream(byteArray);
    }
}
