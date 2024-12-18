using System.Text;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using JsonSerializer = Benzene.Core.Serialization.JsonSerializer;

namespace Benzene.Azure.AspNet;

public static class HttpBuilderExtensions
{
    public static HttpRequest AsAspNetCoreHttpRequest<T>(this IHttpBuilder<T> source)
    {
        return AsAspNetCoreHttpRequest(source, new JsonSerializer());
    }

    public static HttpRequest AsAspNetCoreHttpRequest<T>(this IHttpBuilder<T> source, ISerializer serializer)
    {
        var request = new TestHttpRequest
        {
            Method = source.Method,
            Path = source.Path, 
            Body = ObjectToStream(source.Message, serializer),
            Host = new HostString(""),
            PathBase = new PathString("")
        };

        foreach (var header in source.Headers)
        {
            request.Headers.Add(header.Key, new StringValues(header.Value));
        }

        return request;
    }


    private static MemoryStream ObjectToStream<T>(T obj, ISerializer serializer)
    {
        var byteArray = Encoding.UTF8.GetBytes(serializer.Serialize(obj));
        return new MemoryStream(byteArray);
    }
}
