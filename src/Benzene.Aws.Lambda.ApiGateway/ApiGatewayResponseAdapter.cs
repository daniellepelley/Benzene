using System;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.Helper;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Adapts Benzene's transport-agnostic response handling onto an <see cref="ApiGatewayContext"/>'s
/// <see cref="Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse"/>.
/// </summary>
public class ApiGatewayResponseAdapter : IBenzeneResponseAdapter<ApiGatewayContext>
{
    /// <summary>
    /// Sets a response header, initializing the response if it doesn't already exist.
    /// </summary>
    /// <param name="context">The API Gateway context to set the header on.</param>
    /// <param name="headerKey">The header name.</param>
    /// <param name="headerValue">The header value.</param>
    public void SetResponseHeader(ApiGatewayContext context, string headerKey, string headerValue)
    {
        context.EnsureResponseExists();
        DictionaryUtils.Set(context.ApiGatewayProxyResponse.Headers, headerKey, headerValue);
    }

    /// <summary>
    /// Sets the response's <c>content-type</c> header.
    /// </summary>
    /// <param name="context">The API Gateway context to set the content type on.</param>
    /// <param name="contentType">The content type value.</param>
    public void SetContentType(ApiGatewayContext context, string contentType)
    {
        SetResponseHeader(context, Constants.ContentTypeHeader, contentType);
    }

    /// <summary>
    /// Sets the response status code, initializing the response if it doesn't already exist.
    /// </summary>
    /// <param name="context">The API Gateway context to set the status code on.</param>
    /// <param name="statusCode">The status code as a string (e.g. <c>"200"</c>).</param>
    public void SetStatusCode(ApiGatewayContext context, string statusCode)
    {
        context.EnsureResponseExists();
        context.ApiGatewayProxyResponse.StatusCode = Convert.ToInt32(statusCode);
    }

    /// <summary>
    /// Sets the response body, initializing the response if it doesn't already exist.
    /// </summary>
    /// <param name="context">The API Gateway context to set the body on.</param>
    /// <param name="body">The response body.</param>
    public void SetBody(ApiGatewayContext context, string body)
    {
        context.EnsureResponseExists();
        context.ApiGatewayProxyResponse.Body = body;
    }

    /// <summary>
    /// Sets a raw binary response body, base64-encoding it and flagging the response
    /// <see cref="Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse.IsBase64Encoded"/> so API
    /// Gateway decodes it back to bytes on the way out (the correct path for images, PDFs, and other
    /// binary payloads a handler returns via <see cref="Benzene.Abstractions.Messages.IRawBytesMessage"/>).
    /// </summary>
    /// <param name="context">The API Gateway context to set the body on.</param>
    /// <param name="body">The raw response bytes.</param>
    public void SetBody(ApiGatewayContext context, ReadOnlyMemory<byte> body)
    {
        context.EnsureResponseExists();
        context.ApiGatewayProxyResponse.Body = Convert.ToBase64String(body.Span);
        context.ApiGatewayProxyResponse.IsBase64Encoded = true;
    }

    /// <summary>
    /// Gets the response body, initializing the response if it doesn't already exist.
    /// </summary>
    /// <param name="context">The API Gateway context to read the body from.</param>
    /// <returns>The response body.</returns>
    public string GetBody(ApiGatewayContext context)
    {
        context.EnsureResponseExists();
        return context.ApiGatewayProxyResponse.Body;
    }

    /// <summary>
    /// Finalizes the response. No-op for API Gateway, since the response is returned directly.
    /// </summary>
    /// <param name="context">The API Gateway context being finalized.</param>
    /// <returns>A completed task.</returns>
    public Task FinalizeAsync(ApiGatewayContext context)
    {
        return Task.CompletedTask;
    }
}
