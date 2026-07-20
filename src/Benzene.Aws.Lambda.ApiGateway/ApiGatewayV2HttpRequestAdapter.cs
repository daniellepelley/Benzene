using Benzene.Http;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Adapts an <see cref="ApiGatewayV2Context"/> into Benzene's transport-agnostic <see cref="HttpRequest"/> shape.
/// </summary>
public class ApiGatewayV2HttpRequestAdapter : IHttpRequestAdapter<ApiGatewayV2Context>
{
    /// <summary>
    /// Maps the API Gateway v2 request onto a Benzene <see cref="HttpRequest"/>, reading the method and
    /// path from <c>RequestContext.Http</c> and folding the v2 cookies array into the headers.
    /// </summary>
    /// <param name="context">The API Gateway v2 context to map.</param>
    /// <returns>The mapped HTTP request.</returns>
    public HttpRequest Map(ApiGatewayV2Context context)
    {
        return new HttpRequest
        {
            Path = context.Path,
            Method = context.Method,
            Headers = context.CombinedHeaders()
        };
    }
}
