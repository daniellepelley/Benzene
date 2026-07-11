namespace Benzene.Http;

/// <summary>
/// Provides a configurable implementation of <see cref="IHttpHeaderMappings"/> with custom header mappings.
/// </summary>
/// <remarks>
/// This class allows custom header name mappings to be injected at construction time, enabling
/// transport-specific or application-specific header conventions to be applied consistently
/// across the application.
/// </remarks>
public class HttpHeaderMappings : IHttpHeaderMappings
{
    private readonly IDictionary<string, string> _mappings;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpHeaderMappings"/> class.
    /// </summary>
    /// <param name="mappings">
    /// A dictionary of header name mappings where keys are custom header names and values
    /// are the corresponding HTTP header names.
    /// </param>
    public HttpHeaderMappings(IDictionary<string, string> mappings)
    {
        _mappings = mappings;
    }

    /// <summary>
    /// Gets the header name mappings.
    /// </summary>
    /// <returns>
    /// A dictionary where keys are custom header names and values are the corresponding
    /// HTTP header names to be used in requests and responses.
    /// </returns>
    public IDictionary<string, string> GetMappings()
    {
        return _mappings;
    }
}
