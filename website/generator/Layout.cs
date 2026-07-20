using System.Text;

namespace Benzene.Website.Generator;

internal static class Layout
{
    public static string RenderMarketingPage(string outputPath)
    {
        var css = RepoPaths.RelativeHref(outputPath, "site.css");
        var favicon = RepoPaths.RelativeHref(outputPath, "favicon.svg");
        var docsHome = RepoPaths.RelativeHref(outputPath, "docs/index.html");
        var whyPage = RepoPaths.RelativeHref(outputPath, "why.html");
        var architecturePage = RepoPaths.RelativeHref(outputPath, "architecture.html");
        var operationsPage = RepoPaths.RelativeHref(outputPath, "operations.html");
        var gettingStarted = RepoPaths.RelativeHref(outputPath, "docs/getting-started.html");
        var gettingStartedAws = RepoPaths.RelativeHref(outputPath, "docs/getting-started-aws.html");
        var meshDemo = RepoPaths.RelativeHref(outputPath, "demos/mesh/index.html");
        var specDemo = RepoPaths.RelativeHref(outputPath, "demos/spec/index.html");

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
              <title>Benzene &mdash; one handler, every transport</title>
              <meta name="description" content="A hexagonal (ports-and-adapters) framework for C#. Write a message handler once and reach it over HTTP, SQS, SNS, Kafka, Event Hub and more at the same time &mdash; mix transports on the cloud you already run, with a live service map and a test host for everything you build.">
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
                  <a href="https://www.nuget.org/packages/Benzene.AspNet.Core/"><img src="https://img.shields.io/nuget/v/Benzene.AspNet.Core.svg" alt="NuGet"></a>
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
                    pipeline &mdash; the same pipeline, whichever transport is on the other end, and
                    however many of them at once.
                  </p>
                  <div class="arch-diagram-wrap">{ArchitectureDiagram.Render()}</div>
                </section>

                <section class="section">
                  <h2>Quickstart</h2>
                  {quickstart}
                  <p class="install-line">
                    <code>{Html(MarketingContent.InstallCommand)}</code>
                  </p>
                  <p>
                    Add <code>Benzene.Aws.Lambda.Sqs</code>, <code>.Sns</code>, and
                    <code>.EventBridge</code> the same way for the other transports &mdash;
                    or a different host entirely.
                    <a href="{gettingStartedAws}">See the full AWS walkthrough &rarr;</a>
                  </p>
                </section>

                <section class="section">
                  <h2>And it runs wherever you already are</h2>
                  <p class="section-lede">
                    Cloud portability is the bonus, not the pitch &mdash; most teams pick one
                    platform and stay. The point is that Benzene meets whichever one your platform
                    team already chose: the same handlers run unchanged on any of these, so "which
                    host" stays a deployment detail rather than an architecture decision.
                  </p>
                  <div class="platform-grid">
                    {platforms}
                  </div>
                </section>

                <section class="section">
                  <h2>Try it live</h2>
                  <p class="section-lede">
                    No sign-up, no install &mdash; these are the same self-contained dashboard
                    pages your own Benzene services would serve, running here against sample data.
                  </p>
                  <div class="feature-grid">
                    <div class="feature-card">
                      <h3>Mesh UI</h3>
                      <p>
                        A service-mesh dashboard over sample health checks, contract drift, and
                        cross-service traffic.
                      </p>
                      <p><a href="{meshDemo}">Open the demo &rarr;</a></p>
                    </div>
                    <div class="feature-card">
                      <h3>Spec UI</h3>
                      <p>
                        A Swagger-UI-style browser for a sample Benzene message spec &mdash;
                        topics, payloads, and validation rules.
                      </p>
                      <p><a href="{specDemo}">Open the demo &rarr;</a></p>
                    </div>
                  </div>
                </section>

                <section class="section">
                  <h2>Built for production, not just prototypes</h2>
                  <p class="section-lede">
                    The quickstart is five minutes; the reason to adopt Benzene is what happens
                    after. Three deeper looks, for whoever is asking the question:
                  </p>
                  <div class="feature-grid">
                    <div class="feature-card">
                      <h3>Why Benzene</h3>
                      <p>
                        The case for adopting it &mdash; lower cost of change, less lock-in, quality
                        by construction, built to last. <a href="{whyPage}">Read on &rarr;</a>
                      </p>
                    </div>
                    <div class="feature-card">
                      <h3>Architecture</h3>
                      <p>
                        Ports and adapters applied honestly: handlers, transports, one pipeline, and
                        a service that describes itself. <a href="{architecturePage}">See how it fits &rarr;</a>
                      </p>
                    </div>
                    <div class="feature-card">
                      <h3>Operations</h3>
                      <p>
                        Observability, health, failure handling, and deployment &mdash; what it takes
                        to run it, honestly scoped. <a href="{operationsPage}">Run it in production &rarr;</a>
                      </p>
                    </div>
                  </div>
                </section>

                <p class="cta"><a class="button" href="{docsHome}">Read the docs &rarr;</a></p>
              </main>
              {Footer()}
            </body>
            </html>
            """;
    }

    public static string RenderValuePage(MarketingPages.ValuePage page)
    {
        var outputPath = page.Slug;
        var css = RepoPaths.RelativeHref(outputPath, "site.css");
        var favicon = RepoPaths.RelativeHref(outputPath, "favicon.svg");
        var activeSection = page.Slug[..^".html".Length];

        string RenderCard(MarketingPages.Card card) =>
            $"<div class=\"feature-card\"><h3>{Html(card.Title)}</h3><p>{card.BodyHtml}</p></div>";

        string RenderSection(MarketingPages.Section section) =>
            $"""
             <section class="section">
               <h2>{Html(section.Heading)}</h2>
               <p class="section-lede">{section.LedeHtml}</p>
               <div class="feature-grid">
                 {string.Join("\n", section.Cards.Select(RenderCard))}
               </div>
             </section>
             """;

        var sections = string.Join("\n", page.Sections.Select(RenderSection));

        return $"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{Html(page.Title)} &mdash; Benzene</title>
              <meta name="description" content="{Html(page.Description)}">
              <link rel="icon" href="{favicon}" type="image/svg+xml">
              <link rel="stylesheet" href="{css}">
            </head>
            <body>
              {Header(outputPath, activeSection)}
              <main class="content marketing">
                <section class="page-hero">
                  <h1>{Html(page.Title)}</h1>
                  <p class="section-lede">{page.HeroLedeHtml}</p>
                </section>
                {sections}
                <p class="cta">{page.CtaHtml}</p>
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
              <title>{(title == "Benzene" ? "Benzene Docs" : $"{Html(title)} - Benzene")}</title>
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
        var why = RepoPaths.RelativeHref(outputPath, "why.html");
        var architecture = RepoPaths.RelativeHref(outputPath, "architecture.html");
        var operations = RepoPaths.RelativeHref(outputPath, "operations.html");
        var docs = RepoPaths.RelativeHref(outputPath, "docs/index.html");
        string Active(string section) => activeSection == section ? " class=\"active\"" : "";
        return $"""
            <header class="site-header">
              <a class="brand" href="{home}">{Logo.Inline(28)}<span>Benzene</span></a>
              <nav class="top-nav">
                <a href="{home}"{Active("home")}>Home</a>
                <a href="{why}"{Active("why")}>Why Benzene</a>
                <a href="{architecture}"{Active("architecture")}>Architecture</a>
                <a href="{operations}"{Active("operations")}>Operations</a>
                <a href="{docs}"{Active("docs")}>Docs</a>
                <a href="https://github.com/daniellepelley/Benzene">GitHub</a>
                <a href="https://www.nuget.org/packages/Benzene.AspNet.Core/">NuGet</a>
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
