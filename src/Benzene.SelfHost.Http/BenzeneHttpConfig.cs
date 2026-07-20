namespace Benzene.SelfHost.Http;

public class BenzeneHttpConfig
{
    public string Url { get; set; }

    /// <summary>The maximum number of requests handled concurrently.</summary>
    public int ConcurrentRequests { get; set; }

    /// <summary>
    /// The maximum time <c>StopAsync</c> waits for in-flight requests to finish before abandoning
    /// them and closing the listener.
    /// </summary>
    public TimeSpan DrainTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The maximum request body size, in bytes, that will be read. A request whose body exceeds this
    /// is rejected with a <see cref="RequestBodyTooLargeException"/> before the whole body is buffered,
    /// so an oversized (or lying/chunked) request can't exhaust memory. <c>null</c> (the default)
    /// leaves the body unbounded - the original behavior; set a value in production.
    /// </summary>
    public long? MaxRequestBodyBytes { get; set; }
}