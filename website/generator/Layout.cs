using System.Text;

namespace Benzene.Website.Generator;

internal static class Layout
{
    /// <param name="wiredLanguageIds">
    /// The ids of the language sources this run actually built. The marketing page advertises only
    /// these: a port whose repo checkout is best-effort may be absent, and linking to docs that were
    /// never generated would fail the broken-link self-check and take the whole site build down.
    /// </param>
    public static string RenderMarketingPage(
        string outputPath, IReadOnlyCollection<string> wiredLanguageIds, SiteOptions options)
    {
        const string metaTitle = "Benzene &mdash; one handler, every transport";
        const string metaDescription =
            "A hexagonal (ports-and-adapters) architecture for message-driven services, defined by a "
            + "language-neutral spec and implemented in .NET, Go, TypeScript, and Python. Write a handler "
            + "once and reach it over HTTP, queues, streams and serverless functions at the same time "
            + "&mdash; with a live service map and a test host for everything you build.";
        var css = RepoPaths.RelativeHref(outputPath, "site.css");
        var favicon = RepoPaths.RelativeHref(outputPath, "favicon.svg");
        var docsHome = RepoPaths.RelativeHref(outputPath, "docs/index.html");
        var whyPage = RepoPaths.RelativeHref(outputPath, "why.html");
        var architecturePage = RepoPaths.RelativeHref(outputPath, "architecture.html");
        var operationsPage = RepoPaths.RelativeHref(outputPath, "operations.html");
        var specHome = RepoPaths.RelativeHref(outputPath, "docs/specification/index.html");
        var meshDemo = RepoPaths.RelativeHref(outputPath, "demos/mesh/index.html");
        var specDemo = RepoPaths.RelativeHref(outputPath, "demos/spec/index.html");

        var features = string.Join("\n", MarketingContent.Features.Select(f => $"""
            <div class="feature-card">
              <h3>{Html(f.Title)}</h3>
              <p>{f.Body}</p>
            </div>
            """));

        var languages = MarketingContent.Languages
            .Where(l => wiredLanguageIds.Contains(l.Id))
            .ToList();
        var getStarted = BuildGetStartedSelector(outputPath, languages);
        var heroLangs = string.Join(" &middot;\n", languages.Select(l =>
            $"<strong>{Html(l.Label)}</strong>{(l.Beta ? " <span class=\"beta\">beta</span>" : "")}"));
        var multiLanguageLede = MarketingContent.MultiLanguageLede.Replace("{SPEC}", specHome);

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
              <title>{metaTitle}</title>
              <meta name="description" content="{metaDescription}">
            {SeoHead(options, outputPath, metaTitle, metaDescription, "website")}
              <link rel="icon" href="{favicon}" type="image/svg+xml">
              <link rel="stylesheet" href="{css}">
            </head>
            <body>
              {Header(outputPath, activeSection: "home")}

              <section class="hero">
                {Logo.Inline(96)}
                <h1>Benzene</h1>
                <p class="hero-tagline">{MarketingContent.Tagline}</p>
                <p class="hero-lede">{multiLanguageLede}</p>
                <div class="hero-ctas">
                  <a class="button" href="#get-started">Get started</a>
                  <a class="button button-secondary" href="{docsHome}">Read the docs</a>
                  <a class="button button-secondary" href="https://github.com/daniellepelley/Benzene">View on GitHub</a>
                </div>
                <div class="hero-langs">
                  Ports: {heroLangs}
                  &middot; <a href="https://opensource.org/licenses/MIT">MIT</a>
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

                <section class="section" id="get-started">
                  <h2>Get started</h2>
                  <p class="section-lede">
                    The same handler, in the language you build in. .NET is the reference
                    implementation; Go, TypeScript, and Python are early ports of the same spec.
                  </p>
                  {getStarted}
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

    public static string RenderValuePage(MarketingPages.ValuePage page, SiteOptions options)
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
            {SeoHead(options, outputPath, Html(page.Title), Html(page.Description), "article")}
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

    public static string RenderDocsPage(
        string title, string description, string bodyHtml, NavNode nav, string outputPath,
        DocSource source, IReadOnlyList<DocSource> allSources, SiteOptions options)
    {
        var css = RepoPaths.RelativeHref(outputPath, "site.css");
        var favicon = RepoPaths.RelativeHref(outputPath, "favicon.svg");

        // The <title> keeps its existing shape; the OG/description use a clean page name and a real
        // blurb (the page's first paragraph, or a generic fallback for a page that opens without prose).
        var pageName = title == "Benzene" ? "Benzene Docs" : title;
        var docTitle = title == "Benzene" ? "Benzene Docs" : $"{Html(title)} - Benzene";
        var blurb = Html(string.IsNullOrWhiteSpace(description)
            ? $"{pageName} — {source.Label} documentation for Benzene, a hexagonal architecture for message-driven services."
            : description);

        var sidebar = new StringBuilder();
        sidebar.Append(SectionSwitcher(outputPath, source, allSources));
        sidebar.Append("<ul>");
        foreach (var child in nav.Children) RenderNavNode(child, outputPath, sidebar);
        sidebar.Append("</ul>");

        return $"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{docTitle}</title>
              <meta name="description" content="{blurb}">
            {SeoHead(options, outputPath, Html(pageName), blurb, "article")}
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

    /// <summary>
    /// The cross-language docs hub at docs/index.html: the headline landing that points at the
    /// language-neutral spec and at each language port's own docs home. This is the "same idea, pick
    /// your language" entry the marketing header's Docs link targets.
    /// </summary>
    public static string RenderDocsHubPage(
        string outputPath, IReadOnlyList<DocSource> sources,
        IReadOnlyDictionary<string, List<Page>> pagesBySource, SiteOptions options)
    {
        const string hubDescription =
            "Benzene documentation: the language-neutral specification, and per-language guides for "
            + "building, hosting, testing and operating a Benzene service.";
        var css = RepoPaths.RelativeHref(outputPath, "site.css");
        var favicon = RepoPaths.RelativeHref(outputPath, "favicon.svg");

        var languages = sources.Where(s => s.IsLanguage).ToList();
        var languageCards = string.Join("\n", languages.Select(lang =>
        {
            var home = RepoPaths.RelativeHref(outputPath, lang.HomeOutputPath);
            var count = pagesBySource.TryGetValue(lang.Id, out var pages) ? pages.Count : 0;
            var blurb = lang.LandingOnly
                ? $"An early port &mdash; start with the overview, then the {Html(lang.Label)} repo."
                : $"How to build, host, test and operate a Benzene service in {Html(lang.Label)} &mdash; {count} pages.";
            var cta = lang.LandingOnly ? $"Open the {Html(lang.Label)} overview" : $"Open the {Html(lang.Label)} docs";
            return $"""
                <div class="feature-card">
                  <h3>{Html(lang.Label)}</h3>
                  <p>{blurb}</p>
                  <p><a href="{home}">{cta} &rarr;</a></p>
                </div>
                """;
        }));

        // A section per cross-cutting source (Specification, Guides) — the shared, language-neutral
        // material that leads the hub before the per-language docs.
        var crossCutting = sources.Where(s => !s.IsLanguage).ToList();
        var crossCuttingSections = string.Join("\n", crossCutting.Select(src =>
        {
            if (!pagesBySource.TryGetValue(src.Id, out var pages)) return "";
            var home = RepoPaths.RelativeHref(outputPath, src.HomeOutputPath);
            var links = string.Join("\n", pages
                .Where(p => !string.Equals(Path.GetFileName(p.DocRelativePath), src.NavFile, StringComparison.Ordinal))
                .OrderBy(p => p.Title, StringComparer.Ordinal)
                .Select(p => $"<li><a href=\"{RepoPaths.RelativeHref(outputPath, p.OutputPath)}\">{Html(p.Title)}</a></li>"));
            var (heading, lede) = src.Id switch
            {
                "spec" => ("The specification",
                    "Benzene is defined by a language-neutral specification &mdash; concepts, wire "
                    + "contracts, transport bindings, and conformance fixtures &mdash; that every language "
                    + "port implements. It is the same in every language."),
                "guides" => ("Guides",
                    "Language-neutral guides to Benzene's concepts and tooling, true for every port."),
                "patterns" => ("Patterns",
                    "Recurring ways of composing Benzene's core building blocks into services, the "
                    + "same shape in every language."),
                _ => (Html(src.Label), $"Cross-language {Html(src.Label)}."),
            };
            return $"""
                <section class="section">
                  <h2>{Html(heading)}</h2>
                  <p class="section-lede">{lede} <a href="{home}">Start with the overview &rarr;</a></p>
                  <ul class="hub-spec-list">
                    {links}
                  </ul>
                </section>
                """;
        }));

        return $"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Benzene Documentation</title>
              <meta name="description" content="{hubDescription}">
            {SeoHead(options, outputPath, "Benzene Documentation", hubDescription, "website")}
              <link rel="icon" href="{favicon}" type="image/svg+xml">
              <link rel="stylesheet" href="{css}">
            </head>
            <body>
              {Header(outputPath, activeSection: "docs")}
              <main class="content marketing">
                <section class="page-hero">
                  <h1>Documentation</h1>
                  <p class="section-lede">
                    Start with what Benzene <em>is</em> &mdash; the language-neutral material below
                    &mdash; then drill into the language you build in.
                  </p>
                </section>
                {crossCuttingSections}
                <section class="section">
                  <h2>Pick your language</h2>
                  <p class="section-lede">
                    Each language port is a translation of the same spec. ".NET" is the first; more
                    follow the same shape.
                  </p>
                  <div class="feature-grid">
                    {languageCards}
                  </div>
                </section>
              </main>
              {Footer()}
            </body>
            </html>
            """;
    }

    /// <summary>
    /// The docs-sidebar section control. The specification is a **peer** of the language docs, not a
    /// language: it always shows as its own link (so you can get back to it from any language's docs),
    /// and the languages sit under a separate no-JS dropdown. On a spec page the spec link is active
    /// and the dropdown reads "Language guides"; on a language page that language is active and the
    /// spec link is the way back.
    /// </summary>
    private static string SectionSwitcher(string outputPath, DocSource current, IReadOnlyList<DocSource> allSources)
    {
        var crossCutting = allSources.Where(s => !s.IsLanguage).ToList();
        var languages = allSources.Where(s => s.IsLanguage).ToList();
        if (crossCutting.Count == 0 && languages.Count == 0) return "";

        var sb = new StringBuilder();
        sb.Append("<div class=\"section-switcher\">");

        // The cross-cutting sections (Specification, Guides) are peers, not languages: each is its own
        // link (the way back from any language's docs), marked active when you're in it.
        foreach (var x in crossCutting)
        {
            var href = RepoPaths.RelativeHref(outputPath, x.HomeOutputPath);
            var active = !current.IsLanguage && current.Id == x.Id ? " active" : "";
            sb.Append($"<a class=\"section-link{active}\" href=\"{href}\">{Html(x.Label)}</a>");
        }

        if (languages.Count > 0)
        {
            var items = new StringBuilder();
            foreach (var lang in languages)
            {
                var href = RepoPaths.RelativeHref(outputPath, lang.HomeOutputPath);
                var la = lang.Id == current.Id ? " class=\"active\"" : "";
                items.Append($"<li><a href=\"{href}\"{la}>{Html(lang.Label)}</a></li>");
            }
            var summary = current.IsLanguage
                ? $"<span class=\"lang-switcher-label\">Language:</span> {Html(current.Label)}"
                : "<span class=\"lang-switcher-label\">Language guides</span>";
            var open = current.IsLanguage ? "" : " open";
            sb.Append($"""
                <details class="lang-switcher"{open}>
                  <summary>{summary}</summary>
                  <ul>{items}</ul>
                </details>
                """);
        }

        sb.Append("</div>");
        return sb.ToString();
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
        else if (IsExternal(node.Href))
        {
            // A nav bullet pointing off-site has no page in the output tree to resolve to, so it used
            // to fall through to the group-header branch and render as a dead, uppercased <span> - a
            // link that looked like a heading and did nothing. It is still a link; emit it as one.
            into.Append($"<a href=\"{Html(node.Href!)}\" class=\"nav-external\" rel=\"noopener\">{Html(node.Title)}</a>");
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

    /// <summary>
    /// Whether a nav href points off the generated site. Only http(s) counts: an unresolved
    /// <em>relative</em> href is a broken link in the source, and rendering it as a link would
    /// publish the breakage rather than leave it visible as an un-navigable entry.
    /// </summary>
    private static bool IsExternal(string? href) =>
        href != null &&
        Uri.TryCreate(href, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// A no-JS "get started" selector: radio inputs + labels + CSS <c>:checked</c> reveal one
    /// per-language panel (install + snippet + links). The radios and panels are siblings so the
    /// CSS sibling combinator can drive it. NOTE: `assets/site.css` enumerates the language ids
    /// (dotnet/go/typescript/python) for the tab highlighting — add a new id there when adding a
    /// language. <paramref name="langs"/> is already filtered to the sources this run built.
    /// </summary>
    private static string BuildGetStartedSelector(
        string outputPath, IReadOnlyList<MarketingContent.LanguageStart> langs)
    {
        var inputs = string.Join("\n", langs.Select((l, i) =>
            $"<input type=\"radio\" name=\"gs\" id=\"gs-{l.Id}\" class=\"gs-radio\"{(i == 0 ? " checked" : "")}>"));
        var labels = string.Join("\n", langs.Select(l =>
            $"<label class=\"gs-tab\" for=\"gs-{l.Id}\">{Html(l.Label)}{(l.Beta ? " <span class=\"beta\">beta</span>" : "")}</label>"));
        var panels = string.Join("\n", langs.Select(l =>
        {
            var docs = RepoPaths.RelativeHref(outputPath, l.DocsOutputPath);
            var betaNote = l.Beta
                ? "<p class=\"beta-note\">Early port &mdash; the API is still settling. See the repo for the current state.</p>"
                : "";
            var primary = l.Beta
                ? $"<a href=\"{l.RepoUrl}\">{Html(l.Label)} on GitHub &rarr;</a>"
                : $"<a href=\"{docs}\">Full {Html(l.Label)} walkthrough &rarr;</a>";
            return $$"""
                <div class="gs-panel" data-lang="{{l.Id}}">
                  <p class="gs-install"><code>{{Html(l.Install)}}</code></p>
                  <pre><code>{{l.Code}}</code></pre>
                  {{betaNote}}
                  <p class="gs-links">{{primary}} &middot; <a href="{{docs}}">Docs</a></p>
                </div>
                """;
        }));
        return $"<div class=\"gs-tabs\">{inputs}<div class=\"gs-tablist\">{labels}</div>{panels}</div>";
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

    /// <summary>
    /// A tiny static redirect page emitted at a page's pre-split path (e.g. /docs/getting-started.html)
    /// pointing at its new home (/dotnet/docs/getting-started.html), so old inbound links survive on a
    /// plain static host with no server-side redirect rules. Uses a meta refresh + canonical link, with
    /// a visible fallback link.
    /// </summary>
    public static string RenderRedirectStub(string fromOutputPath, string toOutputPath)
    {
        var href = RepoPaths.RelativeHref(fromOutputPath, toOutputPath);
        return $"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta http-equiv="refresh" content="0; url={href}">
              <meta name="robots" content="noindex">
              <link rel="canonical" href="{href}">
              <title>Moved</title>
            </head>
            <body>
              <p>This page has moved to <a href="{href}">{Html(href)}</a>.</p>
            </body>
            </html>
            """;
    }

    /// <summary>
    /// The absolute URL of an output page, for canonical/OG/sitemap use. The site root is served at
    /// the origin itself (benzene.app/), every other page at its explicit path (kept with the trailing
    /// <c>.html</c> so the URL resolves on a plain static host without directory-index rewriting).
    /// </summary>
    public static string AbsoluteUrl(string baseUrl, string outputPath) =>
        outputPath == "index.html" ? $"{baseUrl}/" : $"{baseUrl}/{outputPath}";

    /// <summary>
    /// The per-page discoverability block: a self-referencing canonical link plus Open Graph and
    /// Twitter Card metadata, so a page shared to Slack/X/LinkedIn/etc renders a real title, blurb and
    /// image instead of a bare URL, and search engines see one canonical address. <paramref name="title"/>
    /// and <paramref name="description"/> must already be attribute-safe (the callers pass the same
    /// values they put in <c>&lt;title&gt;</c>/<c>&lt;meta name="description"&gt;</c>).
    /// </summary>
    private static string SeoHead(
        SiteOptions options, string outputPath, string title, string description, string ogType)
    {
        var url = AbsoluteUrl(options.BaseUrl, outputPath);
        var image = $"{options.BaseUrl}/og-image.svg";
        return $"""
              <link rel="canonical" href="{url}">
              <meta property="og:type" content="{ogType}">
              <meta property="og:site_name" content="Benzene">
              <meta property="og:title" content="{title}">
              <meta property="og:description" content="{description}">
              <meta property="og:url" content="{url}">
              <meta property="og:image" content="{image}">
              <meta property="og:image:type" content="image/svg+xml">
              <meta property="og:image:width" content="1200">
              <meta property="og:image:height" content="630">
              <meta name="twitter:card" content="summary_large_image">
              <meta name="twitter:title" content="{title}">
              <meta name="twitter:description" content="{description}">
              <meta name="twitter:image" content="{image}">{GoogleHead(options)}
        """;
    }

    /// <summary>
    /// The optional Google integrations shared by every page head: a Search Console ownership meta tag
    /// and the Google Analytics (GA4) gtag.js snippet. Each is emitted only when its identifier is
    /// configured, so a build with neither set (local, preview) ships no tracking and no stray tag.
    /// Returned pre-indented to sit inline after the SEO block; empty when nothing is configured.
    /// </summary>
    private static string GoogleHead(SiteOptions options)
    {
        var head = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(options.GoogleSiteVerification))
        {
            head.Append($"""

                  <meta name="google-site-verification" content="{Html(options.GoogleSiteVerification)}">
            """);
        }

        if (!string.IsNullOrWhiteSpace(options.GoogleAnalyticsId))
        {
            // Consent-gated GA4: gtag.js is not requested and no cookie is set until the visitor clicks
            // Accept on the banner. Reject (or no prior choice + navigating away) loads nothing. The
            // decision is remembered in localStorage; a "Cookie preferences" link added to the footer
            // reopens the banner so it can be changed (and denial flips GA's own ga-disable flag so a
            // withdrawal takes effect immediately, even after an earlier accept). The banner markup and
            // the footer toggle are injected here, so a build with no GA id ships neither.
            var id = options.GoogleAnalyticsId;
            head.Append($$"""

                  <!-- Google Analytics (GA4), loaded only after the visitor accepts the cookie banner -->
                  <script>
                    (function () {
                      var GA_ID = '{{JsString(id)}}';
                      var KEY = 'benzene-analytics-consent';
                      function loadGa() {
                        var s = document.createElement('script');
                        s.async = true;
                        s.src = 'https://www.googletagmanager.com/gtag/js?id=' + encodeURIComponent(GA_ID);
                        document.head.appendChild(s);
                        window.dataLayer = window.dataLayer || [];
                        function gtag() { dataLayer.push(arguments); }
                        window.gtag = gtag;
                        gtag('js', new Date());
                        gtag('config', GA_ID);
                      }
                      function save(v) { try { localStorage.setItem(KEY, v); } catch (e) {} }
                      function read() { try { return localStorage.getItem(KEY); } catch (e) { return null; } }
                      function decide(v) {
                        save(v);
                        var el = document.getElementById('cookie-consent');
                        if (el && el.parentNode) { el.parentNode.removeChild(el); }
                        if (v === 'granted') { loadGa(); }
                        else { window['ga-disable-' + GA_ID] = true; }
                      }
                      function banner() {
                        if (document.getElementById('cookie-consent')) { return; }
                        var box = document.createElement('div');
                        box.id = 'cookie-consent';
                        box.setAttribute('role', 'region');
                        box.setAttribute('aria-label', 'Cookie consent');
                        var p = document.createElement('p');
                        p.className = 'cookie-consent-text';
                        p.textContent = 'We use Google Analytics to understand how the site is used. It only sets cookies if you accept.';
                        var actions = document.createElement('div');
                        actions.className = 'cookie-consent-actions';
                        var no = document.createElement('button');
                        no.type = 'button';
                        no.className = 'button button-secondary';
                        no.textContent = 'Reject';
                        no.addEventListener('click', function () { decide('denied'); });
                        var yes = document.createElement('button');
                        yes.type = 'button';
                        yes.className = 'button';
                        yes.textContent = 'Accept';
                        yes.addEventListener('click', function () { decide('granted'); });
                        actions.appendChild(no);
                        actions.appendChild(yes);
                        box.appendChild(p);
                        box.appendChild(actions);
                        document.body.appendChild(box);
                      }
                      function addToggle() {
                        var line = document.querySelector('.site-footer p');
                        if (!line) { return; }
                        var link = document.createElement('button');
                        link.type = 'button';
                        link.className = 'cookie-prefs-link';
                        link.textContent = 'Cookie preferences';
                        link.addEventListener('click', function () {
                          try { localStorage.removeItem(KEY); } catch (e) {}
                          banner();
                        });
                        line.appendChild(document.createTextNode(' · '));
                        line.appendChild(link);
                      }
                      function init() {
                        addToggle();
                        var choice = read();
                        if (choice === 'granted') { loadGa(); }
                        else if (choice !== 'denied') { banner(); }
                      }
                      if (document.readyState === 'loading') {
                        document.addEventListener('DOMContentLoaded', init);
                      } else {
                        init();
                      }
                    })();
                  </script>
            """);
        }

        return head.ToString();
    }

    /// <summary>Escapes a trusted-but-defensive value for embedding in a single-quoted JS string literal.</summary>
    private static string JsString(string text) => text.Replace("\\", "\\\\").Replace("'", "\\'");

    private static string Html(string text) => System.Net.WebUtility.HtmlEncode(text);
}
