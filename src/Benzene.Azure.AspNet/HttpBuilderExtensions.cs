using System.Text;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using JsonSerializer = Benzene.Core.MessageHandlers.Serialization.JsonSerializer;

namespace Benzene.Azure.AspNet;

/// <summary>
/// Provides extension methods for converting an <see cref="IHttpBuilder{T}"/> into an ASP.NET Core
/// <see cref="HttpRequest"/>, for use in tests that exercise the HTTP trigger entry point directly.
/// </summary>
public static class HttpBuilderExtensions
{
    /// <summary>
    /// Builds an ASP.NET Core <see cref="HttpRequest"/> from the builder, serializing its message with a
    /// default JSON serializer.
    /// </summary>
    /// <typeparam name="T">The message payload type.</typeparam>
    /// <param name="source">The HTTP builder describing the request to build.</param>
    /// <returns>The built <see cref="HttpRequest"/> (a <see cref="TestHttpRequest"/> instance).</returns>
    public static HttpRequest AsAspNetCoreHttpRequest<T>(this IHttpBuilder<T> source)
    {
        return AsAspNetCoreHttpRequest(source, new JsonSerializer());
    }

    /// <summary>
    /// Builds an ASP.NET Core <see cref="HttpRequest"/> from the builder, serializing its message with the
    /// given serializer.
    /// </summary>
    /// <typeparam name="T">The message payload type.</typeparam>
    /// <param name="source">The HTTP builder describing the request to build.</param>
    /// <param name="serializer">The serializer used to serialize the message as the request body.</param>
    /// <returns>The built <see cref="HttpRequest"/> (a <see cref="TestHttpRequest"/> instance).</returns>
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
