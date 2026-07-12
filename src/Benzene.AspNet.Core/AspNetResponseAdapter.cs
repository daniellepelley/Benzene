using Benzene.Abstractions.MessageHandlers.Response;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

/// <summary>
/// Adapts Benzene's transport-agnostic response operations onto an <see cref="AspNetContext"/>'s
/// underlying <see cref="HttpContext.Response"/>. Buffers the status code and body internally, writing
/// them to the real response only in <see cref="FinalizeAsync"/>.
/// </summary>
public class AspNetResponseAdapter : IBenzeneResponseAdapter<AspNetContext>
{
    private string _body = string.Empty;

    /// <summary>
    /// The status code written to the response by <see cref="FinalizeAsync"/> if
    /// <see cref="SetStatusCode"/> is never called.
    /// </summary>
    private int _statusCode = 404;

    /// <summary>
    /// Sets a response header on the underlying HTTP response.
    /// </summary>
    /// <param name="context">The context to set the header on.</param>
    /// <param name="headerKey">The header name.</param>
    /// <param name="headerValue">The header value.</param>
    public void SetResponseHeader(AspNetContext context, string headerKey, string headerValue)
    {
        context.HttpContext.Response.Headers.Add(headerKey, headerValue);
    }

    /// <summary>
    /// Sets the content type of the response.
    /// </summary>
    /// <param name="context">The context to set the content type on.</param>
    /// <param name="contentType">The content type.</param>
    public void SetContentType(AspNetContext context, string contentType)
    {
        context.HttpContext.Response.Headers["content-type"] = contentType;
    }

    /// <summary>
    /// Buffers the HTTP status code to be written to the response in <see cref="FinalizeAsync"/>.
    /// </summary>
    /// <param name="context">The context (unused; the status code is buffered on this instance).</param>
    /// <param name="statusCode">The status code, as a numeric string.</param>
    public void SetStatusCode(AspNetContext context, string statusCode)
    {
        _statusCode = Convert.ToInt32(statusCode);
    }

    /// <summary>
    /// Buffers the response body to be written to the response in <see cref="FinalizeAsync"/>.
    /// </summary>
    /// <param name="context">The context (unused; the body is buffered on this instance).</param>
    /// <param name="body">The response body.</param>
    public void SetBody(AspNetContext context, string body)
    {
        _body = body;
    }

    /// <summary>
    /// Gets the currently buffered body.
    /// </summary>
    /// <param name="context">The context (unused; the body is buffered on this instance).</param>
    /// <returns>The buffered response body.</returns>
    public string GetBody(AspNetContext context)
    {
        return _body;
    }

    /// <summary>
    /// Writes the buffered status code and body to the underlying HTTP response and flushes it.
    /// </summary>
    /// <param name="context">The context whose underlying response to finalize.</param>
    public async Task FinalizeAsync(AspNetContext context)
    {
        context.HttpContext.Response.StatusCode = _statusCode;
        await context.HttpContext.Response.WriteAsync(_body);
        await context.HttpContext.Response.Body.FlushAsync();
    }
}
