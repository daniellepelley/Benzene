using System;
using System.Collections.Generic;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Http;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Provides the middleware pipeline context for an API Gateway HTTP API (payload format version 2.0)
/// request, wrapping the raw <see cref="APIGatewayHttpApiV2ProxyRequest"/> and the
/// <see cref="APIGatewayHttpApiV2ProxyResponse"/> being built.
/// </summary>
/// <remarks>
/// The v2 payload shape differs from v1 (<see cref="ApiGatewayContext"/>): the HTTP method and path
/// live under <c>RequestContext.Http</c>, headers are single-value (comma-joined), and cookies arrive
/// as a dedicated <c>Cookies</c> array rather than a <c>Cookie</c> header. This context normalizes
/// those differences so the shared <see cref="IHttpContext"/> pipeline sees them the same way it sees
/// v1 requests.
/// </remarks>
public class ApiGatewayV2Context : IHttpContext, IHasMessageResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayV2Context"/> class.
    /// </summary>
    /// <param name="apiGatewayProxyRequest">The raw API Gateway HTTP API v2 proxy request.</param>
    public ApiGatewayV2Context(APIGatewayHttpApiV2ProxyRequest apiGatewayProxyRequest)
    {
        ApiGatewayProxyRequest = apiGatewayProxyRequest;
    }

    /// <summary>
    /// Gets the raw API Gateway HTTP API v2 proxy request.
    /// </summary>
    public APIGatewayHttpApiV2ProxyRequest ApiGatewayProxyRequest { get; }

    /// <summary>
    /// Gets or sets the API Gateway HTTP API v2 proxy response to return. Populated by response
    /// middleware as the pipeline executes; use <see cref="Extensions.EnsureResponseExists(ApiGatewayV2Context)"/>
    /// to lazily initialize it.
    /// </summary>
    public APIGatewayHttpApiV2ProxyResponse ApiGatewayProxyResponse { get; set; }

    /// <summary>
    /// Gets the request's HTTP method (from <c>RequestContext.Http.Method</c>), or null if absent.
    /// </summary>
    public string Method => ApiGatewayProxyRequest?.RequestContext?.Http?.Method;

    /// <summary>
    /// Gets the request's path (from <c>RequestContext.Http.Path</c>), or null if absent.
    /// </summary>
    public string Path => ApiGatewayProxyRequest?.RequestContext?.Http?.Path;

    /// <summary>
    /// Builds the request's headers, folding the dedicated v2 <c>Cookies</c> array into a single
    /// <c>cookie</c> header (unless one is already present) so the shared HTTP pipeline sees cookies
    /// the same way it does on v1. The returned dictionary is case-insensitive.
    /// </summary>
    /// <returns>A case-insensitive header dictionary including the folded cookie header.</returns>
    /// <summary>
    /// Gets or sets the outcome of handling this request, recorded by
    /// <see cref="ApiGatewayV2MessageHandlerResultSetter"/> so a cross-cutting observer of the completed
    /// pipeline (e.g. metrics) sees a real success/failure signal rather than a missing one.
    /// </summary>
    public IBenzeneResult MessageResult { get; set; } = null!;

    public IDictionary<string, string> CombinedHeaders()
    {
        var headers = ApiGatewayProxyRequest?.Headers != null
            ? new Dictionary<string, string>(ApiGatewayProxyRequest.Headers, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var cookies = ApiGatewayProxyRequest?.Cookies;
        if (cookies is { Length: > 0 } && !headers.ContainsKey("cookie"))
        {
            headers["cookie"] = string.Join("; ", cookies);
        }

        return headers;
    }
}
