namespace Benzene.Http.Routing;

/// <summary>
/// Opt-in policy for path-based HTTP versioning (docs/specification/versioning.md). When enabled via
/// <c>AddHttpVersioning()</c>, every discovered <see cref="HttpEndpointAttribute"/> route is additionally
/// exposed under a version segment (default <c>/v{version}/…</c>), so <c>POST /v1/orders</c> and
/// <c>POST /v2/orders</c> both reach the same topic and the matched <c>version</c> route parameter drives
/// version dispatch / payload upcasting (read by <see cref="HttpMessageVersionGetterBase{TContext}"/>).
/// Absent entirely when versioning is off (the default).
/// </summary>
public class HttpVersioningOptions
{
    /// <summary>
    /// The single route segment prepended to each route to carry the version. Must contain the
    /// <c>{version}</c> route parameter (the name <see cref="HttpMessageVersionGetterBase{TContext}"/> reads).
    /// Default <c>v{version}</c> → <c>/v1/…</c>, <c>/v2/…</c>. Set to e.g. <c>version/{version}</c> for
    /// <c>/version/1/…</c>.
    /// </summary>
    public string VersionSegment { get; set; } = "v{version}";

    /// <summary>
    /// When <c>true</c> (the default) the original unversioned route is kept alongside the versioned one and
    /// resolves to the <em>latest</em> handler (the version selector picks the highest available when no
    /// version is signalled) — so a caller can address <c>/orders</c> (latest) or <c>/v1/orders</c> (pinned).
    /// Set <c>false</c> to require an explicit version segment on every request.
    /// </summary>
    public bool KeepUnversionedRoute { get; set; } = true;

    /// <summary>The route-parameter name the version segment must bind, matching the HTTP version getter.</summary>
    public const string VersionParameterName = "version";
}
