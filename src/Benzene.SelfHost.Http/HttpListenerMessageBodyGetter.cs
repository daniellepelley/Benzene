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
public class HttpListenerMessageBodyGetter : IMessageBodyGetter<SelfHostHttpContext>, IHttpRequestBodyReader<SelfHostHttpContext>
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
    /// Gets the request body. Returns the value buffered by the async pre-read when available;
    /// otherwise reads the input stream synchronously as a fallback.
    /// </summary>
    /// <param name="context">The HTTP context to extract the body from.</param>
    /// <returns>The request body as a string.</returns>
    public string? GetBody(SelfHostHttpContext context)
    {
        return _buffer.IsBuffered
            ? _buffer.Body
            : ReadBodyAsync(context).GetAwaiter().GetResult();
    }

    /// <summary>Reads the request body asynchronously, without blocking a thread.</summary>
    /// <param name="context">The HTTP context to read the body from.</param>
    /// <returns>The request body as a string.</returns>
    public async Task<string?> ReadBodyAsync(SelfHostHttpContext context)
    {
        var request = context.HttpListenerContext.Request;

        if (!_maxRequestBodyBytes.HasValue)
        {
            // Unbounded (the default): original behavior, byte-for-byte.
            using var reader = new StreamReader(request.InputStream);
            return await reader.ReadToEndAsync();
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
        var stream = request.InputStream;
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

        var encoding = request.ContentEncoding ?? System.Text.Encoding.UTF8;
        return encoding.GetString(buffered.GetBuffer(), 0, (int)buffered.Length);
    }
}
