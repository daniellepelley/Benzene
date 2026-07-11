namespace Benzene.Http.Cors;

/// <summary>
/// Provides configuration settings for Cross-Origin Resource Sharing (CORS).
/// </summary>
/// <remarks>
/// CORS is a security mechanism that allows or restricts web applications running in one
/// domain to access resources from another domain. These settings control which origins
/// (domains) and headers are allowed in cross-origin HTTP requests.
/// </remarks>
public class CorsSettings
{
    /// <summary>
    /// Gets or sets the list of allowed domains for CORS requests.
    /// </summary>
    /// <remarks>
    /// Domains should be specified as full URLs (e.g., "https://example.com") or hostnames.
    /// Requests with Origin headers matching these domains will be allowed to access the API.
    /// Use "*" to allow all origins (not recommended for production).
    /// </remarks>
    public string[] AllowedDomains { get; set; }

    /// <summary>
    /// Gets or sets the list of HTTP headers that are allowed in CORS requests.
    /// </summary>
    /// <remarks>
    /// These headers will be included in the Access-Control-Allow-Headers response header.
    /// Common headers include "Content-Type", "Authorization", "X-Requested-With", etc.
    /// </remarks>
    public string[] AllowedHeaders { get; set; }
}