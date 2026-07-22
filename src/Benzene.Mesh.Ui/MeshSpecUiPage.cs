using System.Net;
using System.Reflection;

namespace Benzene.Mesh.Ui;

/// <summary>
/// Provides the self-contained Benzene <b>mesh-hosted</b> Spec Explorer HTML page — a Swagger-UI-style
/// viewer for a single mesh service's Benzene spec, rendered by the mesh itself rather than by the
/// service. It reads the verbatim spec the aggregator already captured into the same-origin
/// <c>services/{name}.json</c> snapshot (its <c>specJson</c> field), so an individual service only
/// ever has to serve its spec as JSON (the Cloud Service contract) — it never has to host any HTML of
/// its own. The page is embedded in this assembly and has no external dependencies, so it can be
/// served by any static file host, or by any Benzene transport.
/// </summary>
/// <remarks>
/// The page is opened with a <c>?service=&lt;name&gt;</c> query-string parameter (and, when the mesh
/// UI was pointed at a non-default manifest, a <c>?manifest=&lt;url&gt;</c> parameter naming where the
/// artifacts live) — this is exactly the link <c>mesh-ui.html</c>'s per-service <em>spec</em> action
/// builds. A <c>?url=&lt;specUrl&gt;</c> parameter is also honoured as a direct-spec fallback. The
/// <see cref="GetHtml(string)"/> overload injects a <c>data-manifest-url</c> so a page opened with no
/// query param still knows where the artifacts live.
/// </remarks>
public static class MeshSpecUiPage
{
    private const string ResourceName = "Benzene.Mesh.Ui.mesh-spec-ui.html";

    private static readonly Lazy<string> LazyHtml = new(ReadResource);

    /// <summary>
    /// Gets the viewer HTML with no manifest URL injected. The page reads the service to show, and
    /// (optionally) where the artifacts live, from its own <c>?service=</c>/<c>?manifest=</c> query
    /// string.
    /// </summary>
    /// <returns>The complete, self-contained HTML document.</returns>
    public static string GetHtml() => LazyHtml.Value;

    /// <summary>
    /// Gets the viewer HTML with a manifest URL injected onto the document root, used as the default
    /// artifact location when the page is opened without a <c>?manifest=</c> query parameter.
    /// </summary>
    /// <param name="manifestUrl">
    /// The URL the page should resolve <c>services/{name}.json</c> against by default. If null or
    /// whitespace, this behaves like <see cref="GetHtml()"/>.
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
        var assembly = typeof(MeshSpecUiPage).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' was not found in assembly '{assembly.FullName}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
