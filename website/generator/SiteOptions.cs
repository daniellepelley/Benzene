namespace Benzene.Website.Generator;

/// <summary>
/// Site-wide build options that aren't tied to any single page or source: the absolute origin used
/// for canonical/OG/sitemap URLs, and the optional Google identifiers injected into every page head.
/// The two Google values are empty by default, so a local or preview build emits no analytics snippet
/// and no verification tag — no tracking, and no stray ownership tag; production passes them in from
/// CI (they are public values, visible in page source, so they travel as CI variables, not secrets).
/// </summary>
public sealed record SiteOptions
{
    /// <summary>The absolute origin the site is served from, no trailing slash (e.g. https://benzene.app).</summary>
    public required string BaseUrl { get; init; }

    /// <summary>A Google Analytics 4 measurement id (e.g. <c>G-XXXXXXXXXX</c>), or empty to emit no analytics.</summary>
    public string GoogleAnalyticsId { get; init; } = "";

    /// <summary>
    /// A Google Search Console <c>google-site-verification</c> token, or empty to emit no verification
    /// meta tag. Only the token is stored — the surrounding <c>&lt;meta&gt;</c> is generated.
    /// </summary>
    public string GoogleSiteVerification { get; init; } = "";
}
