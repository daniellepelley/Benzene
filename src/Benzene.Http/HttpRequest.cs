namespace Benzene.Http;

/// <summary>
/// Represents a simplified, transport-agnostic HTTP request.
/// </summary>
/// <remarks>
/// This class provides a unified representation of HTTP requests across different transport
/// implementations (ASP.NET Core, AWS Lambda, self-hosted servers, etc.). It contains the
/// essential information needed for routing and processing HTTP requests in Benzene.
/// </remarks>
public class HttpRequest
{
    /// <summary>
    /// Gets or sets the HTTP method (GET, POST, PUT, DELETE, PATCH, etc.).
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL path of the request, excluding query string parameters.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP headers included in the request.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}