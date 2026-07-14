namespace Benzene.Abstractions.MessageHandlers.Response;

/// <summary>
/// The write side of a transport-specific outgoing response, giving response handlers (see
/// <see cref="IResponseHandlerContainer{TContext}"/>) a common set of operations for setting
/// headers, content type, status, and body without depending on the transport's native response
/// type directly.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public interface IBenzeneResponseAdapter<TContext>
{
    /// <summary>Sets a header on the outgoing response.</summary>
    /// <param name="context">The transport-specific context to write to.</param>
    /// <param name="headerKey">The header name.</param>
    /// <param name="headerValue">The header value.</param>
    void SetResponseHeader(TContext context, string headerKey, string headerValue);

    /// <summary>Sets the content type of the outgoing response.</summary>
    /// <param name="context">The transport-specific context to write to.</param>
    /// <param name="contentType">The content type (e.g. media type) to set.</param>
    void SetContentType(TContext context, string contentType);

    /// <summary>Sets the status code of the outgoing response.</summary>
    /// <param name="context">The transport-specific context to write to.</param>
    /// <param name="statusCode">The status code to set, in the transport's own status code format.</param>
    void SetStatusCode(TContext context, string statusCode);

    /// <summary>Sets the body of the outgoing response.</summary>
    /// <param name="context">The transport-specific context to write to.</param>
    /// <param name="body">The serialized response body.</param>
    void SetBody(TContext context, string body);

    /// <summary>
    /// Sets the body of the outgoing response from raw bytes, for byte-oriented serialization
    /// (see <see cref="Benzene.Abstractions.Serialization.IPayloadSerializer"/>). The default
    /// implementation decodes as UTF-8 and delegates to <see cref="SetBody(TContext, string)"/>, so
    /// every existing adapter keeps compiling and behaving as before without overriding this member;
    /// an adapter that can genuinely accept bytes (avoiding the decode) may override it.
    /// </summary>
    /// <param name="context">The transport-specific context to write to.</param>
    /// <param name="body">The serialized response body, as UTF-8 bytes.</param>
    void SetBody(TContext context, ReadOnlyMemory<byte> body)
    {
        SetBody(context, System.Text.Encoding.UTF8.GetString(body.Span));
    }

    /// <summary>Gets the body currently set on the outgoing response.</summary>
    /// <param name="context">The transport-specific context to read from.</param>
    /// <returns>The currently-set response body.</returns>
    string GetBody(TContext context);

    /// <summary>
    /// Completes the response after all response handlers have run (e.g. flushing it to the
    /// underlying transport). Called once by <see cref="IResponseHandlerContainer{TContext}"/> after
    /// its response handlers have finished writing to the context.
    /// </summary>
    /// <param name="context">The transport-specific context to finalize.</param>
    Task FinalizeAsync(TContext context);
}