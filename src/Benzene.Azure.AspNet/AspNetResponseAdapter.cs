using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Azure.AspNet;

/// <summary>
/// Adapts Benzene's transport-agnostic response operations onto an <see cref="AspNetContext"/>'s
/// <see cref="Microsoft.AspNetCore.Mvc.ContentResult"/>, creating it lazily on first use.
/// </summary>
public class AspNetResponseAdapter : IBenzeneResponseAdapter<AspNetContext>
{
    /// <summary>
    /// Sets a response header on the underlying HTTP response.
    /// </summary>
    /// <param name="context">The context to set the header on.</param>
    /// <param name="headerKey">The header name.</param>
    /// <param name="headerValue">The header value.</param>
    public void SetResponseHeader(AspNetContext context, string headerKey, string headerValue)
    {
        context.EnsureResponseExists();
        context.HttpRequest.HttpContext.Response.Headers.Add(headerKey, headerValue);
    }

    /// <summary>
    /// Sets the content type of the response.
    /// </summary>
    /// <param name="context">The context to set the content type on.</param>
    /// <param name="contentType">The content type.</param>
    public void SetContentType(AspNetContext context, string contentType)
    {
        context.ContentResult.ContentType = contentType;
    }

    /// <summary>
    /// Sets the HTTP status code of the response.
    /// </summary>
    /// <param name="context">The context to set the status code on.</param>
    /// <param name="statusCode">The status code, as a numeric string.</param>
    public void SetStatusCode(AspNetContext context, string statusCode)
    {
        context.EnsureResponseExists();
        context.ContentResult.StatusCode = Convert.ToInt32(statusCode);
    }

    /// <summary>
    /// Sets the body of the response.
    /// </summary>
    /// <param name="context">The context to set the body on.</param>
    /// <param name="body">The response body.</param>
    public void SetBody(AspNetContext context, string body)
    {
        context.EnsureResponseExists();
        context.ContentResult.Content = body;
    }

    /// <summary>
    /// Gets the current body of the response.
    /// </summary>
    /// <param name="context">The context to get the body from.</param>
    /// <returns>The response body.</returns>
    public string GetBody(AspNetContext context)
    {
        context.EnsureResponseExists();
        return context.ContentResult.Content;
    }

    /// <summary>
    /// Finalizes the response. No-op; the <see cref="Microsoft.AspNetCore.Mvc.ContentResult"/> is
    /// returned directly from the entry point application.
    /// </summary>
    /// <param name="context">The context being finalized.</param>
    /// <returns>A completed task.</returns>
    public Task FinalizeAsync(AspNetContext context)
    {
       return Task.CompletedTask;
    }
}


