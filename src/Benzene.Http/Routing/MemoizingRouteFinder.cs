namespace Benzene.Http.Routing;

/// <summary>
/// A per-request (scoped) <see cref="IRouteFinder"/> decorator that caches the last match, so the
/// several collaborators that each independently resolve the route for one HTTP request - the topic
/// getter, the version getter, and the request enricher (and, when tracing is on, the activity
/// decorator via the topic getter) - don't each re-run <see cref="RouteFinder.Find"/> over the same
/// method+path.
/// </summary>
/// <remarks>
/// Registered scoped, wrapping the singleton <see cref="RouteFinder"/>. Within one request those
/// collaborators run sequentially through the pipeline (never concurrently on one scope), so the
/// single-slot cache needs no synchronization. The cache is a pure memo of a deterministic function
/// (method+path over a fixed route table): a miss simply recomputes, so a differing call is always
/// correct, just uncached.
/// </remarks>
public class MemoizingRouteFinder : IRouteFinder
{
    private readonly IRouteFinder _inner;

    private string? _method;
    private string? _path;
    private HttpTopicRoute? _cached;
    private bool _hasCached;

    /// <summary>Initializes a new instance of the <see cref="MemoizingRouteFinder"/> class.</summary>
    /// <param name="inner">The underlying route finder (the process-wide singleton) to memoize.</param>
    public MemoizingRouteFinder(IRouteFinder inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public HttpTopicRoute? Find(string method, string path)
    {
        if (_hasCached && _method == method && _path == path)
        {
            return _cached;
        }

        _cached = _inner.Find(method, path);
        _method = method;
        _path = path;
        _hasCached = true;
        return _cached;
    }
}
