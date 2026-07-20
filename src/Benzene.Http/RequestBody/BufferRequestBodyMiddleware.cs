using Benzene.Abstractions.Middleware;

namespace Benzene.Http.RequestBody;

/// <summary>
/// Front-of-pipeline middleware that reads the request body <em>asynchronously, once</em> via the
/// transport's <see cref="IHttpRequestBodyReader{TContext}"/> and stores it in the scoped
/// <see cref="HttpRequestBodyBuffer"/>, so the transport's synchronous
/// <see cref="Benzene.Abstractions.Messages.Mappers.IMessageBodyGetter{TContext}"/> can later serve
/// the body from memory instead of blocking a thread-pool thread on a synchronous stream read
/// (<c>ReadToEndAsync().Result</c> / <c>ReadToEnd()</c>).
/// </summary>
/// <typeparam name="TContext">The transport-specific context type.</typeparam>
public class BufferRequestBodyMiddleware<TContext> : IMiddleware<TContext>
{
    private readonly IHttpRequestBodyReader<TContext> _reader;
    private readonly HttpRequestBodyBuffer _buffer;

    /// <summary>Initializes a new instance of the <see cref="BufferRequestBodyMiddleware{TContext}"/> class.</summary>
    /// <param name="reader">The transport-specific async body reader.</param>
    /// <param name="buffer">The scoped per-request buffer to store the read body in.</param>
    public BufferRequestBodyMiddleware(IHttpRequestBodyReader<TContext> reader, HttpRequestBodyBuffer buffer)
    {
        _reader = reader;
        _buffer = buffer;
    }

    /// <inheritdoc />
    public string Name => "BufferRequestBody";

    /// <inheritdoc />
    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        // Prefer the byte path when the transport supports it (binary request bodies): buffer the raw
        // bytes verbatim so a binary body getter can serve them and a string getter derives the text.
        // The default reader returns null here, so string-only transports (ASP.NET) are unchanged.
        var bytes = await _reader.ReadBodyBytesAsync(context);
        if (bytes.HasValue)
        {
            _buffer.SetBytes(bytes.Value);
        }
        else
        {
            _buffer.Set(await _reader.ReadBodyAsync(context));
        }

        await next();
    }
}
