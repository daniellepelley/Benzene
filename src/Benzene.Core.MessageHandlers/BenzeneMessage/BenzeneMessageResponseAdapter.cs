using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Helper;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

/// <summary>
/// The <c>BenzeneMessage</c> transport's <see cref="IBenzeneResponseAdapter{TContext}"/>: reads and
/// writes headers, content type, status code, and body on <see cref="BenzeneMessageContext.BenzeneMessageResponse"/>,
/// creating the response object on first write via the <c>EnsureResponseExists</c> extension method.
/// </summary>
public class BenzeneMessageResponseAdapter : IBenzeneResponseAdapter<BenzeneMessageContext>
{
    /// <inheritdoc />
    public void SetResponseHeader(BenzeneMessageContext context, string headerKey, string headerValue)
    {
        context.EnsureResponseExists();
        DictionaryUtils.Set(context.BenzeneMessageResponse.Headers, headerKey, headerValue);
    }

    /// <inheritdoc />
    public void SetContentType(BenzeneMessageContext context, string contentType)
    {
        SetResponseHeader(context, Constants.ContentTypeHeader, contentType);
    }

    /// <inheritdoc />
    public void SetStatusCode(BenzeneMessageContext context, string statusCode)
    {
        context.EnsureResponseExists();
        context.BenzeneMessageResponse.StatusCode = statusCode;
    }

    /// <inheritdoc />
    public void SetBody(BenzeneMessageContext context, string body)
    {
        context.EnsureResponseExists();
        context.BenzeneMessageResponse.Body = body;
    }

    /// <inheritdoc />
    public string GetBody(BenzeneMessageContext context)
    {
        context.EnsureResponseExists();
        return context.BenzeneMessageResponse.Body;
    }

    /// <summary>
    /// No-op for this transport - there is nothing further to flush after the response object has
    /// been populated.
    /// </summary>
    /// <param name="context">Unused.</param>
    /// <returns>A completed task.</returns>
    public Task FinalizeAsync(BenzeneMessageContext context)
    {
        return Task.CompletedTask;
    }
}
