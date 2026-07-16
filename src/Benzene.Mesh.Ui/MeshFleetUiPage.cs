using System.Net;
using System.Reflection;

namespace Benzene.Mesh.Ui;

/// <summary>
/// The embedded Fleet view page (docs/specification/mesh.md's collector read models rendered
/// live): one self-contained HTML page - no JS framework, no external assets - that polls a
/// collector's <c>mesh:query:fleet</c> topic through a wire-envelope endpoint and renders the
/// derived fleet: services with health and reduced-feed markers, the topic catalog with observed
/// consumers, and recent flows. The dynamic sibling of <see cref="MeshUiPage"/> (which renders the
/// aggregator's published artifacts): this page shows what a live <c>Benzene.Mesh.Collector</c>
/// derives from registrations, heartbeats, and traces - nothing declared.
/// </summary>
public static class MeshFleetUiPage
{
    private const string ResourceName = "Benzene.Mesh.Ui.mesh-fleet-ui.html";

    private static readonly Lazy<string> LazyHtml = new(ReadResource);

    /// <summary>Gets the page with its built-in default envelope URL ("/invoke").</summary>
    public static string GetHtml() => LazyHtml.Value;

    /// <summary>Gets the page pointed at a specific wire-envelope endpoint URL.</summary>
    public static string GetHtml(string envelopeUrl)
    {
        if (string.IsNullOrWhiteSpace(envelopeUrl))
        {
            return LazyHtml.Value;
        }

        var attribute = $" data-envelope-url=\"{WebUtility.HtmlEncode(envelopeUrl)}\"";
        return LazyHtml.Value.Replace("<html lang=\"en\">", $"<html lang=\"en\"{attribute}>");
    }

    private static string ReadResource()
    {
        var assembly = typeof(MeshFleetUiPage).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' was not found in assembly '{assembly.FullName}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
