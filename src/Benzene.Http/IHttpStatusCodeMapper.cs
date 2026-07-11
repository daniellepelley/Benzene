namespace Benzene.Http;

/// <summary>
/// Defines a contract for mapping Benzene result status codes to HTTP status codes.
/// </summary>
/// <remarks>
/// This interface enables consistent HTTP status code handling across different result types
/// by providing a mapping from Benzene's internal result status strings to standard HTTP
/// status code strings (e.g., "200", "404", "500").
/// </remarks>
public interface IHttpStatusCodeMapper
{
    /// <summary>
    /// Maps a Benzene result status to an HTTP status code.
    /// </summary>
    /// <param name="benzeneResultStatus">The Benzene result status string to map.</param>
    /// <returns>The corresponding HTTP status code as a string (e.g., "200", "404", "500").</returns>
    string Map(string benzeneResultStatus);
}
