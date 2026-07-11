namespace Benzene.Http;

/// <summary>
/// Defines a contract for mapping custom header names to standard HTTP header constants.
/// </summary>
/// <remarks>
/// This interface allows transport-agnostic header handling by providing a mapping between
/// custom header names used in the application and the actual HTTP header names. This is
/// particularly useful when different transport implementations use different header conventions.
/// </remarks>
public interface IHttpHeaderMappings
{
    /// <summary>
    /// Gets the header name mappings.
    /// </summary>
    /// <returns>
    /// A dictionary where keys are custom header names and values are the corresponding
    /// HTTP header names to be used in requests and responses.
    /// </returns>
    IDictionary<string, string> GetMappings();
}