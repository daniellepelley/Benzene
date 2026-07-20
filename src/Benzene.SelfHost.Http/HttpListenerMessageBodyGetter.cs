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

    /// <summary>Initializes a new instance of the <see cref="HttpListenerMessageBodyGetter"/> class.</summary>
    /// <param name="buffer">The scoped per-request body buffer.</param>
    public HttpListenerMessageBodyGetter(HttpRequestBodyBuffer buffer)
    {
        _buffer = buffer;
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
        using var reader = new StreamReader(context.HttpListenerContext.Request.InputStream);
        return await reader.ReadToEndAsync();
    }
}
