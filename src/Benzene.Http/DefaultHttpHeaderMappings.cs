namespace Benzene.Http;

/// <summary>
/// Provides the default implementation of <see cref="IHttpHeaderMappings"/> with no custom mappings.
/// </summary>
/// <remarks>
/// This implementation returns an empty dictionary, indicating that no custom header name
/// mappings are applied. Header names will be used as-is without translation.
/// </remarks>
public class DefaultHttpHeaderMappings : IHttpHeaderMappings
{
    // Shared empty instance rather than a fresh allocation per call: the ApiGateway/self-host header
    // paths call GetMappings() per request (twice), and every caller treats the result as read-only.
    private static readonly IDictionary<string, string> Empty = new Dictionary<string, string>();

    /// <summary>
    /// Gets an empty header mappings dictionary.
    /// </summary>
    /// <returns>An empty dictionary indicating no custom header name mappings. Treat as read-only.</returns>
    public IDictionary<string, string> GetMappings()
    {
        return Empty;
    }
}
