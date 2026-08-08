using Benzene.Website.Generator;

var repoRoot = Directory.GetCurrentDirectory();
var outDir = "website/dist";
string? dotnetDocs = null;               // override: the benzene-dotnet checkout's docs root (CI)
var baseUrl = "https://benzene.app";      // site origin for canonical/OG/sitemap absolute URLs
var googleAnalyticsId = "";               // GA4 measurement id (G-XXXXXXXXXX); empty → no analytics
var googleSiteVerification = "";          // Search Console ownership token; empty → no verification tag
var extraSources = new List<DocSource>(); // future languages via --source id=label=urlPrefix=path[=navFile]

for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--out" && i + 1 < args.Length)
    {
        outDir = args[++i];
    }
    else if (args[i] == "--base-url" && i + 1 < args.Length)
    {
        // The absolute origin the site is served from (no trailing slash), used for canonical links,
        // Open Graph/Twitter URLs, and sitemap.xml. Defaults to production; a preview/dev build can
        // point it at dev.benzene.app so those absolute URLs match where the page actually lives.
        baseUrl = args[++i].TrimEnd('/');
    }
    else if (args[i] == "--google-analytics-id" && i + 1 < args.Length)
    {
        // A GA4 measurement id (G-XXXXXXXXXX). When set, every page gets the gtag.js snippet; when
        // absent or empty the site ships no analytics at all, so local and preview builds don't track.
        googleAnalyticsId = args[++i].Trim();
    }
    else if (args[i] == "--google-site-verification" && i + 1 < args.Length)
    {
        // The token from a Google Search Console "HTML tag" verification. When set, every page carries
        // the <meta name="google-site-verification"> tag proving ownership of the domain to Google.
        googleSiteVerification = args[++i].Trim();
    }
    else if (args[i] == "--repo-root" && i + 1 < args.Length)
    {
        repoRoot = args[++i];
    }
    else if (args[i] == "--dotnet-docs" && i + 1 < args.Length)
    {
        dotnetDocs = args[++i];
    }
    else if (args[i] == "--source" && i + 1 < args.Length)
    {
        // A language port, "::"-delimited (so repo URLs, which contain ":", are safe):
        //   id::Label::urlPrefix::docsRootPath[::<extra>...]
        // The 4 required fields come first. Any extra token is matched order-independently: "landing"
        // → render only the README as a single landing page; an "http..." token → the repo blob base
        // URL (unresolved links point there); anything else → the nav file name.
        var parts = args[++i].Split("::");
        if (parts.Length < 4)
        {
            Console.Error.WriteLine(
                "error: --source expects id::Label::urlPrefix::docsRootPath[::navFile][::landing][::<repoBlobUrl>]");
            return 1;
        }
        var landing = false;
        string? navFile = null;
        string? repoBlobUrl = null;
        foreach (var extra in parts.Skip(4))
        {
            if (extra == "landing") landing = true;
            else if (extra.StartsWith("http", StringComparison.OrdinalIgnoreCase)) repoBlobUrl = extra;
            else navFile = extra;
        }
        extraSources.Add(new DocSource
        {
            Id = parts[0],
            Label = parts[1],
            UrlPrefix = parts[2],
            DocsRootDisk = Path.GetFullPath(parts[3]),
            NavFile = navFile ?? (landing ? "README.md" : "index.md"),
            IsLanguage = true,
            LandingOnly = landing,
            RepoBlobUrl = repoBlobUrl,
        });
    }
}

if (!Path.IsPathRooted(outDir))
{
    outDir = Path.Combine(repoRoot, outDir);
}

var specRoot = Path.Combine(repoRoot, "docs", "specification");
if (!File.Exists(Path.Combine(repoRoot, "README.md")) || !Directory.Exists(specRoot))
{
    Console.Error.WriteLine(
        $"error: '{repoRoot}' doesn't look like the benzene repo root (no README.md / docs/specification found). " +
        "Run this from the repo root, or pass --repo-root <path>.");
    return 1;
}

// The .NET docs source: from a benzene-dotnet checkout in CI (--dotnet-docs), or, for local dev
// before the split lands, from benzene's own docs/ (excluding the spec, which is its own source).
var dotnetDocsRoot = dotnetDocs != null ? Path.GetFullPath(dotnetDocs) : Path.Combine(repoRoot, "docs");
var dotnetSource = new DocSource
{
    Id = "dotnet",
    Label = ".NET",
    UrlPrefix = "dotnet/docs",
    DocsRootDisk = dotnetDocsRoot,
    NavFile = "index.md",
    IsLanguage = true,
    LegacyUrlPrefix = "docs",   // .NET docs were at /docs/* pre-split; redirect old links to /dotnet/docs/*
    ExcludedSubdirs = ["specification/", "plans/"],
    ExcludedFiles = ["DOCUMENTATION_QUICK_REFERENCE.md"],
    // The .NET docs link to real repo files not published on the site (example projects, SAM
    // templates). Point those at the file on GitHub — the blob URL of this source's docs root — so
    // they don't render as dead relative links that 403 on the static host. (docs root, because a
    // link like ../examples/… is resolved relative to the page, then normalized against this base.)
    RepoBlobUrl = "https://github.com/daniellepelley/benzene-dotnet/blob/main/docs",
};

// The cross-language spec source (stays in benzene). Not a language: it does not appear in the
// language switcher; it is the headline the docs hub leads with.
var specSource = new DocSource
{
    Id = "spec",
    Label = "Specification",
    UrlPrefix = "docs/specification",
    DocsRootDisk = specRoot,
    NavFile = "index.md",   // a proper nested-list nav (README.md uses tables the nav builder can't read)
    IsLanguage = false,
};

var sources = new List<DocSource> { specSource };

// Cross-language guides (docs/guides/) — language-neutral explanations that aren't part of the
// normative spec. A cross-cutting source like the spec (not a language); optional.
var guidesRoot = Path.Combine(repoRoot, "docs", "guides");
if (File.Exists(Path.Combine(guidesRoot, "index.md")))
{
    sources.Add(new DocSource
    {
        Id = "guides",
        Label = "Guides",
        UrlPrefix = "docs/guides",
        DocsRootDisk = guidesRoot,
        NavFile = "index.md",
        IsLanguage = false,
    });
}

// Cross-language patterns (docs/patterns/) — recurring ways of composing the core building blocks
// into services. Like guides, a cross-cutting source (not a language); optional.
var patternsRoot = Path.Combine(repoRoot, "docs", "patterns");
if (File.Exists(Path.Combine(patternsRoot, "index.md")))
{
    sources.Add(new DocSource
    {
        Id = "patterns",
        Label = "Patterns",
        UrlPrefix = "docs/patterns",
        DocsRootDisk = patternsRoot,
        NavFile = "index.md",
        IsLanguage = false,
    });
}

// The .NET docs are the site's reference section and the marketing/front pages link into them, so
// they are required. They come from an explicit --dotnet-docs (a benzene-dotnet checkout), or —
// before the split's cutover, for a local run — benzene's own docs/index.md. Post-cutover benzene no
// longer carries them, so a bare run must be told where they are.
if (dotnetDocs != null || File.Exists(Path.Combine(dotnetDocsRoot, "index.md")))
{
    sources.Add(dotnetSource);
}
else
{
    Console.Error.WriteLine(
        "error: no .NET docs found. The .NET docs are the site's reference section and the marketing " +
        "pages link into them. Pass --dotnet-docs <path to a benzene-dotnet checkout's docs/>.");
    return 1;
}

sources.AddRange(extraSources);

var options = new SiteOptions
{
    BaseUrl = baseUrl,
    GoogleAnalyticsId = googleAnalyticsId,
    GoogleSiteVerification = googleSiteVerification,
};

return new SiteBuilder(repoRoot, outDir, sources, options).Run();
