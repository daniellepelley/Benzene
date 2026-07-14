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
    public static string GetHtml(string manifestUrl)
    {
        if (string.IsNullOrWhiteSpace(manifestUrl))
        {
            return LazyHtml.Value;
        }

        var attribute = $" data-manifest-url=\"{WebUtility.HtmlEncode(manifestUrl)}\"";
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
