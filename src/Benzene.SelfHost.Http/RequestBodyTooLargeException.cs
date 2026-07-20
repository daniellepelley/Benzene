using System;

namespace Benzene.SelfHost.Http;

/// <summary>
/// Thrown by <see cref="HttpListenerMessageBodyGetter"/> when a request body exceeds
/// <see cref="BenzeneHttpConfig.MaxRequestBodyBytes"/> - either declared up front via
/// <c>Content-Length</c> or discovered while reading a chunked body. Reading stops before the whole
/// body is buffered, so an oversized request can't exhaust process memory.
/// </summary>
public class RequestBodyTooLargeException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="RequestBodyTooLargeException"/> class.</summary>
    /// <param name="bodyBytes">The (declared or observed) body size that exceeded the limit.</param>
    /// <param name="limitBytes">The configured maximum body size.</param>
    public RequestBodyTooLargeException(long bodyBytes, long limitBytes)
        : base($"Request body of {bodyBytes} bytes exceeds the configured limit of {limitBytes} bytes.")
    {
        BodyBytes = bodyBytes;
        LimitBytes = limitBytes;
    }

    /// <summary>Gets the (declared or observed) body size that exceeded the limit.</summary>
    public long BodyBytes { get; }

    /// <summary>Gets the configured maximum body size, in bytes.</summary>
    public long LimitBytes { get; }
}
