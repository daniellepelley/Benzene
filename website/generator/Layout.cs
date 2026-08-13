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
            "Benzene gives every transport what a modern web framework gives HTTP: a middleware "
            + "pipeline, per-request scoping, and handlers you can test. Write a handler once and "
            + "reach it over HTTP, queues, streams and serverless functions at the same time &mdash; "
            + "with a live service map and a test host for everything you build.";
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
    /// The cross-language docs hub at docs/index.html — the page the header's "Docs" link targets,
    /// and (because search engines don't use the front door) the page a large share of visitors meet
    /// Benzene on for the very first time.
    /// <para>
    /// It is therefore ordered for someone who has never heard of Benzene, not for the spec's own
    /// audience: it must <b>say what Benzene is in its first sentence</b>, show the architecture
    /// diagram, and put "pick your language" first. The specification comes last, under a heading
    /// that says plainly who needs it &mdash; it is normative material for people implementing or
    /// verifying a port, and leading with "Porting Guide" / "Conformance Fixtures" reads as an
    /// academic standards project to a developer who just wants to write a handler. Rationale and
    /// the cold-visitor evidence behind this ordering:
    /// <c>work/website-information-architecture-strategy.md</c>.
    /// </para>
    /// </summary>
    public static string RenderDocsHubPage(
        string outputPath, IReadOnlyList<DocSource> sources,
        IReadOnlyDictionary<string, List<Page>> pagesBySource, SiteOptions options)
    {
        const string hubDescription =
            "Benzene gives every transport what a modern web framework gives HTTP: a middleware "
            + "pipeline, per-request scoping, and handlers you can test. HTTP, SQS, and Kafka "
            + "normally mean a different service each; with Benzene you build one. Guides for each "
            + "language, and the language-neutral specification they all implement.";
        var css = RepoPaths.RelativeHref(outputPath, "site.css");
        var favicon = RepoPaths.RelativeHref(outputPath, "favicon.svg");

        var languages = sources.Where(s => s.IsLanguage).ToList();

        // The per-language "where do I actually start" page is already catalogued in
        // MarketingContent (it is what the home page's get-started selector links to), so reuse it
        // rather than guessing at a page title. Falling back to the source's docs home keeps a
        // language wired only via --source from emitting a link to a page that was never generated.
        string StartHref(DocSource lang)
        {
            var entry = MarketingContent.Languages.FirstOrDefault(l => l.Id == lang.Id);
            return RepoPaths.RelativeHref(outputPath, entry?.DocsOutputPath ?? lang.HomeOutputPath);
        }

        var primary = languages.FirstOrDefault(l => !l.LandingOnly) ?? languages.FirstOrDefault();
        var startCta = primary is null
            ? ""
            : $"""
               <p class="hub-cta">
                 <a class="button" href="{StartHref(primary)}">Start building in {Html(primary.Label)}</a>
               </p>
               <p class="hub-note">
                 Benzene is pre-1.0 &mdash; packages are published as prerelease, and the docs mark
                 anything partial or planned as such.
               </p>
               """;

        // "Pick your language" over a single card reads as a broken page, and the surrounding copy
        // then promises a choice the build can't offer. CI's sibling-port checkouts are best-effort
        // (see MarketingContent.Languages), so how many languages a run wires is not fixed — the
        // copy has to degrade with the same filter the cards do.
        var (languageHeading, languageLede) = languages.Count > 1
            ? ("Pick your language",
               "Benzene is one design with an idiomatic implementation per language, so the "
               + "concepts below are the same whichever you choose.")
            : ("Build it in " + Html(primary?.Label ?? "your language"),
               "Benzene is defined by a language-neutral specification and implemented as idiomatic "
               + "ports. " + Html(primary?.Label ?? "This port") + " is the reference implementation; "
               + "Go, TypeScript and Python are early ports of the same spec and will appear here as "
               + "their docs land.");

        var languageCards = string.Join("\n", languages.Select(lang =>
        {
            var home = RepoPaths.RelativeHref(outputPath, lang.HomeOutputPath);
            // Deliberately not "N pages": a page count reads as a warning about how much there is to
            // get through, not as a promise of what you get.
            var blurb = lang.LandingOnly
                ? $"An early port &mdash; start with the overview, then the {Html(lang.Label)} repo."
                : $"Build your first service, host it, test it without the cloud, and run it in "
                  + $"production &mdash; in {Html(lang.Label)}.";
            var badge = lang.Id == "dotnet" ? " <span class=\"card-tag\">reference</span>" : "";
            var links = lang.LandingOnly
                ? $"<a href=\"{home}\">Open the {Html(lang.Label)} overview &rarr;</a>"
                : $"<a href=\"{StartHref(lang)}\">Start here &rarr;</a> &middot; "
                  + $"<a href=\"{home}\">Browse the {Html(lang.Label)} docs</a>";
            return $"""
                <div class="feature-card">
                  <h3>{Html(lang.Label)}{badge}</h3>
                  <p>{blurb}</p>
                  <p>{links}</p>
                </div>
                """;
        }));

        string CrossCuttingSection(DocSource src, string heading, string lede)
        {
            if (!pagesBySource.TryGetValue(src.Id, out var pages)) return "";
            var home = RepoPaths.RelativeHref(outputPath, src.HomeOutputPath);
            var links = string.Join("\n", pages
                .Where(p => !string.Equals(Path.GetFileName(p.DocRelativePath), src.NavFile, StringComparison.Ordinal))
                .OrderBy(p => p.Title, StringComparer.Ordinal)
                .Select(p => $"<li><a href=\"{RepoPaths.RelativeHref(outputPath, p.OutputPath)}\">{Html(p.Title)}</a></li>"));
            return $"""
                <section class="section">
                  <h2>{Html(heading)}</h2>
                  <p class="section-lede">{lede} <a href="{home}">Start with the overview &rarr;</a></p>
                  <ul class="hub-spec-list">
                    {links}
                  </ul>
                </section>
                """;
        }

        // Cross-language material, split by who it is for. Guides and patterns are for people
        // *using* Benzene, so they sit with the language docs; the spec is normative material for
        // people *implementing* it, so it goes last.
        var learnSections = string.Join("\n", sources
            .Where(s => !s.IsLanguage && s.Id != "spec")
            .Select(src => src.Id switch
            {
                "guides" => CrossCuttingSection(src, "Guides",
                    "Language-neutral guides to Benzene's concepts and tooling, true for every port."),
                "patterns" => CrossCuttingSection(src, "Patterns",
                    "Recurring ways of composing Benzene's core building blocks into services, the "
                    + "same shape in every language."),
                _ => CrossCuttingSection(src, src.Label, $"Cross-language {Html(src.Label)}."),
            }));

        var specSection = string.Join("\n", sources
            .Where(s => s.Id == "spec")
            .Select(src => CrossCuttingSection(src, "The specification",
                "<strong>You don't need this to build a service.</strong> Benzene is defined by a "
                + "language-neutral specification &mdash; concepts, wire contracts, transport "
                + "bindings, and conformance fixtures &mdash; that every language port implements. "
                + "Read it if you're porting Benzene to a new language, verifying conformance, or "
                + "you want the normative detail behind something in the guides.")));

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
                  <h1>Benzene documentation</h1>
                  <p class="section-lede">
                    <strong>Benzene gives every transport what a modern web framework gives
                    HTTP &mdash; a middleware pipeline, per-request scoping, and handlers you can
                    test.</strong> An HTTP request, an SQS message, and a Kafka event are
                    fundamentally different things, and normally that means building a different
                    service for each. With Benzene you write one handler and reach it over all of
                    them at once &mdash; you add a transport in the host wiring, never in your logic.
                  </p>
                  {startCta}
                </section>
                <section class="section">
                  <div class="arch-diagram-wrap">{ArchitectureDiagram.Render()}</div>
                </section>
                <section class="section">
                  <h2>{languageHeading}</h2>
                  <p class="section-lede">{languageLede}</p>
                  <div class="feature-grid">
                    {languageCards}
                  </div>
                </section>
                {learnSections}
                {specSection}
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
            // Always offer *this language's* repo. The header's GitHub link points at the
            // cross-language home (daniellepelley/Benzene), which is not where the code in this
            // panel lives — a reader who clicks out expecting the snippet they just read lands in
            // the wrong repository. (The non-beta branch also used to link the same docs page twice.)
            var links = l.Beta
                ? $"<a href=\"{l.RepoUrl}\">{Html(l.Label)} on GitHub &rarr;</a> &middot; "
                  + $"<a href=\"{docs}\">Docs</a>"
                : $"<a href=\"{docs}\">Full {Html(l.Label)} walkthrough &rarr;</a> &middot; "
                  + $"<a href=\"{l.RepoUrl}\">{Html(l.Label)} on GitHub</a>";
            return $$"""
                <div class="gs-panel" data-lang="{{l.Id}}">
                  <p class="gs-install"><code>{{Html(l.Install)}}</code></p>
                  <pre><code>{{l.Code}}</code></pre>
                  {{betaNote}}
                  <p class="gs-links">{{links}}</p>
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
