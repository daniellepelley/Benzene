using Benzene.Http;

namespace Benzene.Azure.Function.AspNet;

/// <summary>
/// Adapts an <see cref="AspNetContext"/>'s ASP.NET Core HTTP request into Benzene's transport-agnostic
/// <see cref="HttpRequest"/> shape.
/// </summary>
public class AspNetHttpRequestAdapter : IHttpRequestAdapter<AspNetContext>
{
    /// <summary>
    /// Maps the context's ASP.NET Core request into a Benzene <see cref="HttpRequest"/>, lower-casing
    /// header names.
    /// </summary>
    /// <param name="context">The context to adapt.</param>
    /// <returns>The adapted HTTP request.</returns>
    public HttpRequest Map(AspNetContext context)
    {
        return new HttpRequest
        {
            Path = context.HttpRequest.Path.Value,
            Method = context.HttpRequest.Method,
            Headers = context.HttpRequest.Headers.ToDictionary(x =>x.Key.ToLowerInvariant(), x => x.Value.ToString())
        };
    }
}
