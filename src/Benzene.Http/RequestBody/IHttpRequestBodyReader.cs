using System;

namespace Benzene.Http.RequestBody;

/// <summary>
/// Transport-specific asynchronous reader for an HTTP request body. Implemented by the transports
/// whose body lives behind a network stream (ASP.NET Core, Azure Functions ASP.NET, the self-hosted
/// HttpListener) so <see cref="BufferRequestBodyMiddleware{TContext}"/> can read that body
/// <em>without blocking a thread</em>, once, at the front of the pipeline, and stash it in the scoped
/// <see cref="HttpRequestBodyBuffer"/> for the synchronous
/// <see cref="Benzene.Abstractions.Messages.Mappers.IMessageBodyGetter{TContext}"/> to serve.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type.</typeparam>
public interface IHttpRequestBodyReader<in TContext>
{
    /// <summary>Reads the request body from the context's stream asynchronously.</summary>
    /// <param name="context">The context to read the body from.</param>
    /// <returns>The body text, or <c>null</c> if there is no readable body or reading it fails.</returns>
    Task<string?> ReadBodyAsync(TContext context);

    /// <summary>
    /// Reads the request body as raw bytes, for transports that support binary request bodies. The
    /// default returns <c>null</c>, meaning "no byte path — use <see cref="ReadBodyAsync"/>"; a
    /// transport that can supply raw bytes (e.g. the self-hosted HttpListener) overrides this so
    /// <see cref="BufferRequestBodyMiddleware{TContext}"/> buffers bytes verbatim and a binary body
    /// getter can serve them without a lossy string round-trip.
    /// </summary>
    /// <param name="context">The context to read the body from.</param>
    /// <returns>The raw body bytes, or <c>null</c> to fall back to the string read.</returns>
    Task<ReadOnlyMemory<byte>?> ReadBodyBytesAsync(TContext context) => Task.FromResult<ReadOnlyMemory<byte>?>(null);
}
