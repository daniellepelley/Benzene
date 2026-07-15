using System.Text;

namespace Benzene.Website.Generator;

internal static class Layout
{
    public static string RenderMarketingPage(string outputPath)
    {
        var css = RepoPaths.RelativeHref(outputPath, "site.css");
        var favicon = RepoPaths.RelativeHref(outputPath, "favicon.svg");
        var docsHome = RepoPaths.RelativeHref(outputPath, "docs/index.html");
        var gettingStarted = RepoPaths.RelativeHref(outputPath, "docs/getting-started.html");

        var features = string.Join("\n", MarketingContent.Features.Select(f => $"""
            <div class="feature-card">
              <h3>{Html(f.Title)}</h3>
              <p>{f.Body}</p>
            </div>
            """));

        var quickstart = string.Join("\n", MarketingContent.QuickstartSteps.Select(s => $"""
            <div class="quickstart-step">
              <p class="quickstart-label">{Html(s.Label)}</p>
              <pre><code>{s.Code}</code></pre>
            </div>
            """));

        var platforms = string.Join("\n", MarketingContent.Platforms.Select(p => $"""
            <div class="platform-pill">
              <strong>{Html(p.Name)}</strong>
              <span>{Html(p.Detail)}</span>
            </div>
            """));

        return $"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Benzene &mdash; one middleware pipeline, every cloud</title>
              <meta name="description" content="A hexagonal (ports-and-adapters) framework for C# built around a shared middleware pipeline. Write your message handlers once, run them on AWS, Azure, Google Cloud, Cloudflare, Kubernetes, or ASP.NET Core, and swap transports with minimal reconfiguration.">
              <link rel="icon" href="{favicon}" type="image/svg+xml">
              <link rel="stylesheet" href="{css}">
            </head>
            <body>
              {Header(outputPath, activeSection: "home")}

              <section class="hero">
                {Logo.Inline(96)}
                <h1>Benzene</h1>
                <p class="hero-tagline">{MarketingContent.Tagline}</p>
                <div class="hero-ctas">
                  <a class="button" href="{gettingStarted}">Get started in 5 minutes</a>
                  <a class="button button-secondary" href="https://github.com/daniellepelley/Benzene">View on GitHub</a>
                </div>
                <div class="hero-badges">
                  <a href="https://github.com/daniellepelley/Benzene/actions"><img src="https://github.com/daniellepelley/Benzene/actions/workflows/build-benzene.yml/badge.svg" alt="Build Status"></a>
                  <a href="https://codecov.io/gh/daniellepelley/Benzene"><img src="https://codecov.io/gh/daniellepelley/Benzene/graph/badge.svg" alt="codecov"></a>
                  <a href="https://www.nuget.org/packages/Benzene/"><img src="https://img.shields.io/nuget/v/Benzene.svg" alt="NuGet"></a>
                  <a href="https://opensource.org/licenses/MIT"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>
                </div>
              </section>

              <main class="content marketing">
                <section class="section">
                  <h2>Why Benzene?</h2>
                  <div class="feature-grid">
                    {features}
                  </div>
                </section>

                <section class="section">
                  <h2>The core idea</h2>
                  <p class="section-lede">
                    Benzene separates <em>what your service does</em> from <em>how it's invoked</em>.
                    A message handler contains your logic. A transport turns an incoming request
                    into a message and routes it to the matching handler through the middleware
                    pipeline &mdash; the same pipeline, whichever cloud is on the other end.
                  </p>
                  <div class="arch-diagram-wrap">{ArchitectureDiagram.Render()}</div>
                </section>

                <section class="section">
                  <h2>Quickstart</h2>
                  {quickstart}
                  <p class="install-line">
                    <code>{Html(MarketingContent.InstallCommand)}</code>
                  </p>
                  <p><a href="{gettingStarted}">See the full five-minute walkthrough &rarr;</a></p>
                </section>

                <section class="section">
                  <h2>Runs everywhere you need it to</h2>
                  <p class="section-lede">
                    And that's only half of it: wherever it runs, the same handler can be
                    reached over HTTP, Lambda events, SQS, SNS, Kafka, EventBridge, Event Hub,
                    Service Bus, or gRPC &mdash; swap between them with minimal reconfiguration,
                    not a rewrite.
                  </p>
                  <div class="platform-grid">
                    {platforms}
                  </div>
                </section>

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
              <a class="brand" href="{home}">{Logo.Inline(28)}<span>Benzene</span></a>
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
