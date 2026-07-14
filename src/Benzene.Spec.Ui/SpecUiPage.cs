using System.Net;
using System.Reflection;

namespace Benzene.Spec.Ui;

/// <summary>
/// Provides the self-contained Benzene Spec Explorer HTML page — a Swagger-UI-style viewer for
/// the Benzene message spec (topics, request/response payloads, broadcast events, and validation
/// rules). The page is embedded in this assembly and has no external dependencies, so it can be
/// served by any transport, not just ASP.NET Core.
/// </summary>
/// <remarks>
/// The page loads a spec from, in order of precedence: a <c>?url=</c> query-string parameter, the
/// <c>data-spec-url</c> attribute on the document root (see <see cref="GetHtml(string)"/>), or an
/// embedded sample. Point it at a running service's spec endpoint — for example
/// <c>/spec?type=benzene</c> (the <c>benzene</c> format produced by <c>UseSpec</c>).
/// </remarks>
public static class SpecUiPage
{
    private const string ResourceName = "Benzene.Spec.Ui.spec-ui.html";

    private static readonly Lazy<string> LazyHtml = new(ReadResource);

    /// <summary>
    /// Gets the viewer HTML with no spec URL injected. The page falls back to its embedded sample
    /// spec unless a <c>?url=</c> query-string parameter is supplied by the browser.
    /// </summary>
    /// <returns>The complete, self-contained HTML document.</returns>
    public static string GetHtml() => LazyHtml.Value;

    /// <summary>
    /// Gets the viewer HTML with a spec URL injected onto the document root, so the page fetches and
    /// renders that spec on load.
    /// </summary>
    /// <param name="specUrl">
    /// The URL the page should fetch the Benzene spec JSON from (for example
    /// <c>/spec?type=benzene</c>). If null or whitespace, this behaves like <see cref="GetHtml()"/>.
    /// </param>
    /// <returns>The complete, self-contained HTML document.</returns>
    public static string GetHtml(string specUrl)
    {
        if (string.IsNullOrWhiteSpace(specUrl))
        {
            return LazyHtml.Value;
        }

        var attribute = $" data-spec-url=\"{WebUtility.HtmlEncode(specUrl)}\"";
        return LazyHtml.Value.Replace("<html lang=\"en\">", $"<html lang=\"en\"{attribute}>");
    }

    private static string ReadResource()
    {
        var assembly = typeof(SpecUiPage).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' was not found in assembly '{assembly.FullName}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
