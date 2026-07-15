using System.Text;

namespace Benzene.Website.Generator;

internal static class Layout
{
    // A plain hexagon ring - the actual benzene molecule's structure - doubles as the wordmark's icon.
    private const string HexagonSvg = """
        <svg class="brand-mark" viewBox="0 0 100 100" width="28" height="28" aria-hidden="true">
          <polygon points="50,4 93,27 93,73 50,96 7,73 7,27" fill="none" stroke="currentColor" stroke-width="6"/>
          <line x1="50" y1="4" x2="50" y2="26" stroke="currentColor" stroke-width="4"/>
          <line x1="93" y1="27" x2="73" y2="39" stroke="currentColor" stroke-width="4"/>
          <line x1="93" y1="73" x2="73" y2="61" stroke="currentColor" stroke-width="4"/>
          <line x1="50" y1="96" x2="50" y2="74" stroke="currentColor" stroke-width="4"/>
          <line x1="7" y1="73" x2="27" y2="61" stroke="currentColor" stroke-width="4"/>
          <line x1="7" y1="27" x2="27" y2="39" stroke="currentColor" stroke-width="4"/>
        </svg>
        """;

    public static string RenderMarketingPage(string title, string bodyHtml, string outputPath)
    {
        var css = RepoPaths.RelativeHref(outputPath, "site.css");
        var favicon = RepoPaths.RelativeHref(outputPath, "favicon.svg");
        var docsHome = RepoPaths.RelativeHref(outputPath, "docs/index.html");

        return $"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{Html(title)}</title>
              <link rel="icon" href="{favicon}" type="image/svg+xml">
              <link rel="stylesheet" href="{css}">
            </head>
            <body>
              {Header(outputPath, activeSection: "home")}
              <main class="content marketing">
                {bodyHtml}
                <p class="cta"><a class="button" href="{docsHome}">Read the docs &rarr;</a></p>
              </main>
              {Footer()}
            </body>
            </html>
            """;
    }

    public static string RenderDocsPage(string title, string bodyHtml, NavNode nav, string outputPath)
    {
        var css = RepoPaths.RelativeHref(outputPath, "site.css");
        var favicon = RepoPaths.RelativeHref(outputPath, "favicon.svg");

        var sidebar = new StringBuilder();
        sidebar.Append("<ul>");
        foreach (var child in nav.Children) RenderNavNode(child, outputPath, sidebar);
        sidebar.Append("</ul>");

        return $"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{Html(title)} - Benzene</title>
              <link rel="icon" href="{favicon}" type="image/svg+xml">
              <link rel="stylesheet" href="{css}">
            </head>
            <body>
              {Header(outputPath, activeSection: "docs")}
              <div class="layout">
                <nav class="sidebar">{sidebar}</nav>
                <main class="content">{bodyHtml}</main>
              </div>
              {Footer()}
            </body>
            </html>
            """;
    }

    private static void RenderNavNode(NavNode node, string fromOutputPath, StringBuilder into)
    {
        into.Append("<li>");
        if (node.OutputHref != null)
        {
            var href = RepoPaths.RelativeHref(fromOutputPath, node.OutputHref);
            var isActive = node.OutputHref == fromOutputPath;
            into.Append($"<a href=\"{href}\"{(isActive ? " class=\"active\"" : "")}>{Html(node.Title)}</a>");
        }
        else
        {
            into.Append($"<span class=\"nav-group\">{Html(node.Title)}</span>");
        }

        if (node.Children.Count > 0)
        {
            into.Append("<ul>");
            foreach (var child in node.Children) RenderNavNode(child, fromOutputPath, into);
            into.Append("</ul>");
        }
        into.Append("</li>");
    }

    private static string Header(string outputPath, string activeSection)
    {
        var home = RepoPaths.RelativeHref(outputPath, "index.html");
        var docs = RepoPaths.RelativeHref(outputPath, "docs/index.html");
        return $"""
            <header class="site-header">
              <a class="brand" href="{home}">{HexagonSvg}<span>Benzene</span></a>
              <nav class="top-nav">
                <a href="{home}"{(activeSection == "home" ? " class=\"active\"" : "")}>Home</a>
                <a href="{docs}"{(activeSection == "docs" ? " class=\"active\"" : "")}>Docs</a>
                <a href="https://github.com/daniellepelley/Benzene">GitHub</a>
                <a href="https://www.nuget.org/packages/Benzene/">NuGet</a>
              </nav>
            </header>
            """;
    }

    private static string Footer()
    {
        return """
            <footer class="site-footer">
              <p>Benzene is released under the MIT License. &middot;
                <a href="https://github.com/daniellepelley/Benzene">Source on GitHub</a></p>
            </footer>
            """;
    }

    private static string Html(string text) => System.Net.WebUtility.HtmlEncode(text);
}
