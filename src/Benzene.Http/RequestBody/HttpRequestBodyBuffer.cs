using System;

namespace Benzene.Http.RequestBody;

/// <summary>
/// Scoped, per-request holder for a message body that has already been read from the transport's
/// request stream. It exists so the request body can be read <em>asynchronously, once</em>, at the
/// front of the pipeline (by <see cref="BufferRequestBodyMiddleware{TContext}"/>) and then served
/// <em>synchronously</em> to the rest of the pipeline via the transport's
/// <see cref="Benzene.Abstractions.Messages.Mappers.IMessageBodyGetter{TContext}"/> - without that
/// getter having to block a thread-pool thread on <c>ReadToEndAsync().Result</c> / a synchronous
/// stream read.
/// </summary>
/// <remarks>
/// This follows the same "scoped DI state, not context" pattern as <c>PresetTopicHolder</c>: a
/// transport context type stays a pure description of the message, and this per-request buffering
/// state lives in a small scoped holder resolved from the same DI scope instead. Registered scoped,
/// so each request gets its own instance.
/// </remarks>
public class HttpRequestBodyBuffer
{
    private string? _body;
    private ReadOnlyMemory<byte> _bodyBytes;

    /// <summary>
    /// Whether the body has been read and stored. When <c>false</c>, nothing has buffered the body
    /// for this request (e.g. <see cref="BufferRequestBodyMiddleware{TContext}"/> was not wired in),
    /// so a body getter should fall back to reading the stream itself. <c>true</c> whether the body
    /// was buffered as a string (<see cref="Set"/>) or as bytes (<see cref="SetBytes"/>).
    /// </summary>
    public bool IsBuffered { get; private set; }

    /// <summary>
    /// Whether the body was buffered as raw bytes (<see cref="SetBytes"/>) rather than as a decoded
    /// string. When <c>true</c>, <see cref="BodyBytes"/> carries the verbatim request bytes (for a
    /// binary body getter), and a string body getter derives the text from them.
    /// </summary>
    public bool IsBytesBuffered { get; private set; }

    /// <summary>The buffered body, or <c>null</c> if the request had no readable body. Only meaningful when <see cref="IsBuffered"/> is <c>true</c> and <see cref="IsBytesBuffered"/> is <c>false</c>.</summary>
    public string? Body => _body;

    /// <summary>The buffered raw body bytes. Only meaningful when <see cref="IsBytesBuffered"/> is <c>true</c>.</summary>
    public ReadOnlyMemory<byte> BodyBytes => _bodyBytes;

    /// <summary>Stores the body read for this request and marks it <see cref="IsBuffered"/>.</summary>
    /// <param name="body">The body text read from the request stream, or <c>null</c> if there was none.</param>
    public void Set(string? body)
    {
        _body = body;
        IsBuffered = true;
    }

    /// <summary>
    /// Stores the raw body bytes read for this request, marking both <see cref="IsBuffered"/> and
    /// <see cref="IsBytesBuffered"/> - so a binary body getter serves the bytes verbatim and a string
    /// body getter can decode them.
    /// </summary>
    /// <param name="bytes">The raw body bytes read from the request stream.</param>
    public void SetBytes(ReadOnlyMemory<byte> bytes)
    {
        _bodyBytes = bytes;
        IsBytesBuffered = true;
        IsBuffered = true;
    }
}
