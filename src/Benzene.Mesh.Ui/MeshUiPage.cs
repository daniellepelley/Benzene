using System.Net;
using System.Reflection;

namespace Benzene.Mesh.Ui;

/// <summary>
/// Provides the self-contained Benzene Mesh Explorer HTML page — a catalog viewer for a
/// service mesh's <c>manifest.json</c>/<c>services/{name}.json</c> artifacts (as published by
/// <c>Benzene.Mesh.Aggregator</c>). The page is embedded in this assembly and has no external
/// dependencies, so it can be served by any static file host, or by any Benzene transport.
/// </summary>
/// <remarks>
/// The page loads a manifest from, in order of precedence: a <c>?url=</c> query-string parameter,
/// the <c>data-manifest-url</c> attribute on the document root (see
/// <see cref="GetHtml(string)"/>), a relative fetch of <c>manifest.json</c>, or an embedded
/// sample. The primary deployment target is a plain static file host serving this page alongside
/// the aggregator's generated <c>manifest.json</c> - it does not require a Benzene pipeline at
/// all, and needs no query param or attribute either, since the relative fetch covers that case.
/// </remarks>
public static class MeshUiPage
{
    private const string ResourceName = "Benzene.Mesh.Ui.mesh-ui.html";

    private static readonly Lazy<string> LazyHtml = new(ReadResource);

    /// <summary>
    /// Gets the viewer HTML with no manifest URL injected. The page tries a <c>?url=</c>
    /// query-string parameter, then a relative fetch of <c>manifest.json</c>, before falling back
    /// to its embedded sample manifest.
    /// </summary>
    /// <returns>The complete, self-contained HTML document.</returns>
    public static string GetHtml() => LazyHtml.Value;

    /// <summary>
    /// Gets the viewer HTML with a manifest URL injected onto the document root, so the page
    /// fetches and renders that manifest on load.
    /// </summary>
    /// <param name="manifestUrl">
    /// The URL the page should fetch <c>manifest.json</c> from. If null or whitespace, this
    /// behaves like <see cref="GetHtml()"/>.
    /// </param>
    /// <returns>The complete, self-contained HTML document.</returns>
    public static string GetHtml(string manifestUrl) => GetHtml(manifestUrl, null);

    /// <summary>
    /// Gets the viewer HTML with a manifest URL and (optionally) a live fleet-envelope endpoint
    /// injected onto the document root. With <paramref name="envelopeUrl"/> set, the page's Fleet
    /// plane feature-detects (via the injected <c>data-fleet-url</c>) and enriches the catalog with
    /// live <c>mesh:query:*</c> data polled from that wire-envelope endpoint; without it the page is
    /// the static catalog viewer exactly as <see cref="GetHtml(string)"/>.
    /// </summary>
    /// <param name="manifestUrl">
    /// The URL the page should fetch <c>manifest.json</c> from. If null or whitespace, no
    /// <c>data-manifest-url</c> is injected (the page falls back to <c>?url=</c>/relative fetch).
    /// </param>
    /// <param name="envelopeUrl">
    /// The wire-envelope endpoint the Fleet plane polls (same-origin path or absolute URL). If null
    /// or whitespace, no <c>data-fleet-url</c> is injected and the Fleet plane stays dormant.
    /// </param>
    /// <returns>The complete, self-contained HTML document.</returns>
    public static string GetHtml(string? manifestUrl, string? envelopeUrl)
    {
        var attribute = string.Empty;
        if (!string.IsNullOrWhiteSpace(manifestUrl))
        {
            attribute += $" data-manifest-url=\"{WebUtility.HtmlEncode(manifestUrl)}\"";
        }

        if (!string.IsNullOrWhiteSpace(envelopeUrl))
        {
            attribute += $" data-fleet-url=\"{WebUtility.HtmlEncode(envelopeUrl)}\"";
        }

        if (attribute.Length == 0)
        {
            return LazyHtml.Value;
        }

        return LazyHtml.Value.Replace("<html lang=\"en\">", $"<html lang=\"en\"{attribute}>");
    }

    private static string ReadResource()
    {
        var assembly = typeof(MeshUiPage).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' was not found in assembly '{assembly.FullName}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
