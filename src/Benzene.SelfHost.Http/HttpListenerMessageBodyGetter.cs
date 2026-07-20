using System;
using System.IO;
using System.Text;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Http.RequestBody;

namespace Benzene.SelfHost.Http;

/// <summary>
/// Extracts the message body from a self-hosted <see cref="System.Net.HttpListener"/> request. Serves
/// the body from the scoped <see cref="HttpRequestBodyBuffer"/> when it has been read up front
/// (asynchronously, by <see cref="BufferRequestBodyMiddleware{SelfHostHttpContext}"/> - auto-wired by
/// <c>UseHttp(...)</c>); implements <see cref="IHttpRequestBodyReader{SelfHostHttpContext}"/> to do
/// that async read. Only if nothing buffered the body (the middleware was not wired in) does it fall
/// back to reading the stream itself.
/// </summary>
/// <remarks>
/// The body is read once as raw bytes and buffered verbatim, so a binary request (a handler whose
/// request type is <c>RawBytesRequest</c>) gets the exact bytes via
/// <see cref="IMessageBodyBytesGetter{SelfHostHttpContext}"/>, and a text request gets the string
/// decoded from those same bytes (via the request's <c>Content-Encoding</c>, defaulting to UTF-8).
/// </remarks>
public class HttpListenerMessageBodyGetter : IMessageBodyGetter<SelfHostHttpContext>, IMessageBodyBytesGetter<SelfHostHttpContext>, IHttpRequestBodyReader<SelfHostHttpContext>
{
    private readonly HttpRequestBodyBuffer _buffer;
    private readonly long? _maxRequestBodyBytes;

    /// <summary>Initializes a new instance of the <see cref="HttpListenerMessageBodyGetter"/> class.</summary>
    /// <param name="buffer">The scoped per-request body buffer.</param>
    public HttpListenerMessageBodyGetter(HttpRequestBodyBuffer buffer)
        : this(buffer, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="HttpListenerMessageBodyGetter"/> class.</summary>
    /// <param name="buffer">The scoped per-request body buffer.</param>
    /// <param name="maxRequestBodyBytes">The maximum body size to read, or <c>null</c> for unbounded.</param>
    public HttpListenerMessageBodyGetter(HttpRequestBodyBuffer buffer, long? maxRequestBodyBytes)
    {
        _buffer = buffer;
        _maxRequestBodyBytes = maxRequestBodyBytes;
    }

    /// <summary>
    /// Gets the request body as a string. Serves the buffered body when available (decoding the
    /// buffered bytes with the request's encoding); otherwise reads the stream synchronously.
    /// </summary>
    /// <param name="context">The HTTP context to extract the body from.</param>
    /// <returns>The request body as a string.</returns>
    public string? GetBody(SelfHostHttpContext context)
    {
        if (_buffer.IsBytesBuffered)
        {
            return Decode(context, _buffer.BodyBytes);
        }

        if (_buffer.IsBuffered)
        {
            return _buffer.Body;
        }

        return Decode(context, ReadRawBytesAsync(context).GetAwaiter().GetResult());
    }

    /// <summary>
    /// Gets the request body as raw bytes, for a binary request. Serves the buffered bytes when
    /// available; otherwise reads the stream synchronously.
    /// </summary>
    /// <param name="context">The HTTP context to extract the body bytes from.</param>
    /// <returns>The request body's raw bytes.</returns>
    public ReadOnlyMemory<byte> GetBodyBytes(SelfHostHttpContext context)
    {
        if (_buffer.IsBytesBuffered)
        {
            return _buffer.BodyBytes;
        }

        if (_buffer.IsBuffered)
        {
            return _buffer.Body is null
                ? ReadOnlyMemory<byte>.Empty
                : GetEncoding(context).GetBytes(_buffer.Body);
        }

        return ReadRawBytesAsync(context).GetAwaiter().GetResult();
    }

    /// <summary>Reads the request body as a string asynchronously, without blocking a thread.</summary>
    /// <param name="context">The HTTP context to read the body from.</param>
    /// <returns>The request body as a string.</returns>
    public async Task<string?> ReadBodyAsync(SelfHostHttpContext context)
    {
        return Decode(context, await ReadRawBytesAsync(context));
    }

    /// <summary>Reads the request body as raw bytes asynchronously (the binary path), without blocking a thread.</summary>
    /// <param name="context">The HTTP context to read the body from.</param>
    /// <returns>The request body's raw bytes.</returns>
    public async Task<ReadOnlyMemory<byte>?> ReadBodyBytesAsync(SelfHostHttpContext context)
    {
        return await ReadRawBytesAsync(context);
    }

    private async Task<ReadOnlyMemory<byte>> ReadRawBytesAsync(SelfHostHttpContext context)
    {
        var request = context.HttpListenerContext.Request;
        var stream = request.InputStream;

        if (!_maxRequestBodyBytes.HasValue)
        {
            // Unbounded (the default).
            using var all = new MemoryStream();
            await stream.CopyToAsync(all);
            return all.ToArray();
        }

        var limit = _maxRequestBodyBytes.Value;

        // Reject up front when Content-Length declares an oversized body...
        if (request.ContentLength64 > limit)
        {
            throw new RequestBodyTooLargeException(request.ContentLength64, limit);
        }

        // ...and enforce the cap while reading, so a chunked or lying Content-Length can't exceed it
        // (we stop before buffering the whole body).
        using var buffered = new MemoryStream();
        var chunk = new byte[8192];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(chunk.AsMemory())) > 0)
        {
            total += read;
            if (total > limit)
            {
                throw new RequestBodyTooLargeException(total, limit);
            }

            buffered.Write(chunk, 0, read);
        }

        return buffered.ToArray();
    }

    private static string Decode(SelfHostHttpContext context, ReadOnlyMemory<byte> bytes)
    {
        return GetEncoding(context).GetString(bytes.Span);
    }

    private static Encoding GetEncoding(SelfHostHttpContext context)
    {
        return context.HttpListenerContext.Request.ContentEncoding ?? System.Text.Encoding.UTF8;
    }
}
