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
    /// <summary>
    /// Gets an empty header mappings dictionary.
    /// </summary>
    /// <returns>An empty dictionary indicating no custom header name mappings.</returns>
    public IDictionary<string, string> GetMappings()
    {
        return new Dictionary<string, string>();
    }
}
