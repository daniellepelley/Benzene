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
}