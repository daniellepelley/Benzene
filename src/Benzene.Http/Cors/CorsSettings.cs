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
    /// A preflight request that asks for a header not in this list (via
    /// <c>Access-Control-Request-Headers</c>) is treated as not allowed. Use <c>"*"</c> to allow
    /// any header; the middleware then echoes back whatever the preflight actually requested
    /// (equivalent to ASP.NET Core's <c>AllowAnyHeader()</c>), since a literal <c>"*"</c> is not
    /// honored by browsers on credentialed requests.
    /// </remarks>
    public string[] AllowedHeaders { get; set; }

    /// <summary>
    /// Gets or sets the list of response headers that browser-side JavaScript is allowed to read
    /// (beyond the small set of CORS-safelisted headers exposed by default, e.g. Content-Type).
    /// </summary>
    /// <remarks>
    /// Sent as the <c>Access-Control-Expose-Headers</c> header on actual (non-preflight)
    /// responses. Equivalent to ASP.NET Core's <c>WithExposedHeaders(...)</c>. Leave empty/null
    /// to expose only the default safelisted headers.
    /// </remarks>
    public string[] ExposedHeaders { get; set; }

    /// <summary>
    /// Gets or sets how long, in seconds, browsers may cache a preflight (OPTIONS) response
    /// before sending another one.
    /// </summary>
    /// <remarks>
    /// When set, this value is sent as the <c>Access-Control-Max-Age</c> header on preflight
    /// responses for allowed origins. Leave <c>null</c> to omit the header, which means
    /// browsers fall back to their own (usually short) default preflight cache duration.
    /// </remarks>
    public int? MaxAgeSeconds { get; set; }

    /// <summary>
    /// Gets or sets whether the response should include <c>Access-Control-Allow-Credentials: true</c>,
    /// permitting cross-origin requests to include cookies or HTTP authentication.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>false</c>. This is safe to combine with a wildcard entry in
    /// <see cref="AllowedDomains"/> because the middleware always echoes back the specific
    /// request's <c>Origin</c> value rather than a literal <c>"*"</c>, which is what the CORS
    /// specification requires when credentials are allowed.
    /// </remarks>
    public bool AllowCredentials { get; set; }
}