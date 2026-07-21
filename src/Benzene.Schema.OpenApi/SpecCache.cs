using System.Collections.Concurrent;

namespace Benzene.Schema.OpenApi;

/// <summary>
/// Process-lifetime memoization of generated spec documents, keyed by (type, format).
/// </summary>
/// <remarks>
/// A spec is derived entirely from startup registrations - message handlers, HTTP endpoints,
/// transports, validation rules - none of which change while the process runs, so the same
/// <see cref="SpecRequest"/> always produces the same document. Without this, every spec request
/// re-runs the full build: Swashbuckle schema generation over every request/response CLR type,
/// example-payload generation, and serialization. That is cheap once but expensive when repeated -
/// exactly what the mesh aggregator's periodic polling of each service's <c>spec</c> endpoint does.
/// This is the same "definitions don't change for the process lifetime" assumption the handler- and
/// endpoint-finder caches (<c>CacheMessageHandlersFinder</c>/<c>CacheHttpEndpointFinder</c>) already
/// rely on, applied one level up at the finished document. Registered as a singleton by
/// <c>UseSpec</c>; the key space is tiny (a few type/format combinations), so memory is negligible.
/// </remarks>
public class SpecCache
{
    // Lazy<T> so a burst of concurrent first-hits for the same (type, format) builds the document
    // once rather than each thread racing an identical, expensive build.
    private readonly ConcurrentDictionary<string, Lazy<string>> _cache = new();

    /// <summary>
    /// Returns the memoized document for <paramref name="request"/>, building it via
    /// <paramref name="build"/> on the first request for that (type, format).
    /// </summary>
    public string GetOrBuild(SpecRequest request, Func<SpecRequest, string> build)
    {
        // Mirror how SpecBuilder discriminates the request so the key never conflates two requests
        // that produce different documents: Type is matched case-insensitively (the builder lower-
        // cases it), but Format is matched exactly (`Format == "yaml"`), so "YAML" (which the builder
        // treats as JSON) must NOT share a key with "yaml". Type is lower-cased here; Format is kept
        // verbatim.
        var key = $"{request.Type?.ToLowerInvariant() ?? string.Empty}|{request.Format ?? string.Empty}";
        return _cache.GetOrAdd(key, _ => new Lazy<string>(() => build(request))).Value;
    }
}
