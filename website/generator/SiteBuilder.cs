using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Benzene.Website.Generator;

internal sealed class SiteBuilder
{
    private readonly string _repoRoot;
    private readonly string _outDir;
    private readonly IReadOnlyList<DocSource> _sources;
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public SiteBuilder(string repoRoot, string outDir, IReadOnlyList<DocSource> sources)
    {
        _repoRoot = repoRoot;
        _outDir = outDir;
        _sources = sources;
    }

    public int Run()
    {
        // 1. Discover every page across every source. Pages are indexed by their absolute disk path so
        //    a link is resolved to the file it points at regardless of which source owns it (a .NET doc
        //    linking into the spec resolves cross-source automatically).
        var pagesByDisk = new Dictionary<string, Page>(StringComparer.Ordinal);
        var pagesBySource = new Dictionary<string, List<Page>>(StringComparer.Ordinal);

        foreach (var source in _sources)
        {
            var pages = DiscoverSource(source);
            pagesBySource[source.Id] = pages;
            foreach (var page in pages)
            {
                pagesByDisk[NormalizeDisk(page.SourceDiskPath)] = page;
            }
        }

        // 2. Build each source's sidebar nav from its own nav file (index.md / README.md).
        var navBySource = new Dictionary<string, NavNode>(StringComparer.Ordinal);
        foreach (var source in _sources)
        {
            navBySource[source.Id] = BuildSourceNav(source, pagesBySource[source.Id], pagesByDisk);
        }

        var languages = _sources.Where(s => s.IsLanguage).ToList();

        // 3. Rewrite links in every page against the global disk index.
        var assetsToCopy = new Dictionary<string, string>(StringComparer.Ordinal); // outputPath -> diskPath
        foreach (var page in pagesByDisk.Values)
        {
            RewriteLinks(page, pagesByDisk, assetsToCopy);
        }

        CopyDemos();
        Directory.CreateDirectory(_outDir);

        // 4. Render every docs page under its source's nav + the language switcher.
        foreach (var source in _sources)
        {
            var nav = navBySource[source.Id];
            foreach (var page in pagesBySource[source.Id])
            {
                var html = RenderPage(page, nav, languages);
                WriteOutput(page.OutputPath, html);
            }
        }

        // 5. The cross-language docs hub (docs/index.html): the headline landing that points at the
        //    spec and at each language's own docs home. The marketing header's "Docs" link targets it.
        WriteOutput("docs/index.html", Layout.RenderDocsHubPage("docs/index.html", _sources, pagesBySource));

        // 5b. Redirect stubs so links to a source's pre-split paths still resolve (e.g. the .NET docs
        //     moved from /docs/* to /dotnet/docs/*). A legacy path already occupied by a real page
        //     (the hub, a spec page) is left alone.
        var occupied = new HashSet<string>(pagesByDisk.Values.Select(p => p.OutputPath), StringComparer.Ordinal)
        {
            "docs/index.html",
        };
        var redirectPaths = new List<string>();
        foreach (var source in _sources)
        {
            if (string.IsNullOrEmpty(source.LegacyUrlPrefix) || source.LegacyUrlPrefix == source.UrlPrefix) continue;
            foreach (var page in pagesBySource[source.Id])
            {
                var rel = page.OutputPath[(source.UrlPrefix.Length + 1)..]; // path within the source
                var legacyPath = $"{source.LegacyUrlPrefix}/{rel}";
                if (occupied.Contains(legacyPath)) continue;
                WriteOutput(legacyPath, Layout.RenderRedirectStub(legacyPath, page.OutputPath));
                redirectPaths.Add(legacyPath);
            }
        }

        // 6. Hand-authored marketing home + value pages (not markdown-derived) at the site root.
        File.WriteAllText(Path.Combine(_outDir, "index.html"), Layout.RenderMarketingPage("index.html"));
        foreach (var valuePage in MarketingPages.All)
        {
            File.WriteAllText(Path.Combine(_outDir, valuePage.Slug), Layout.RenderValuePage(valuePage));
        }

        CopyStaticAssets(assetsToCopy);

        var outputPaths = pagesByDisk.Values.Select(p => p.OutputPath)
            .Append("index.html")
            .Append("docs/index.html")
            .Concat(redirectPaths)
            .Concat(MarketingPages.All.Select(p => p.Slug));
        var brokenLinks = SelfCheck(outputPaths);
        if (brokenLinks.Count > 0)
        {
            Console.Error.WriteLine($"Generation failed: {brokenLinks.Count} broken internal link(s):");
            foreach (var (file, href) in brokenLinks)
            {
                Console.Error.WriteLine($"  {file} -> {href}");
            }
            return 1;
        }

        var pageCount = pagesByDisk.Count + 2 + MarketingPages.All.Length;
        Console.WriteLine(
            $"Generated {pageCount} pages (+{redirectPaths.Count} redirects) from {_sources.Count} source(s) to {_outDir}");
        return 0;
    }

    private List<Page> DiscoverSource(DocSource source)
    {
        if (!Directory.Exists(source.DocsRootDisk))
        {
            throw new InvalidOperationException(
                $"Doc source '{source.Id}' docs root not found: {source.DocsRootDisk}");
        }

        var pages = new List<Page>();
        foreach (var disk in Directory.EnumerateFiles(source.DocsRootDisk, "*.md", SearchOption.AllDirectories))
        {
            var docRel = RepoPaths.Normalize(Path.GetRelativePath(source.DocsRootDisk, disk));
            if (source.ExcludedFiles.Contains(docRel, StringComparer.Ordinal)) continue;
            if (source.ExcludedSubdirs.Any(prefix => docRel.StartsWith(prefix, StringComparison.Ordinal))) continue;

            var text = File.ReadAllText(disk);
            var document = Markdown.Parse(text, _pipeline);
            var outputPath = $"{source.UrlPrefix}/{docRel[..^".md".Length]}.html";
            var page = new Page
            {
                Source = source,
                DocRelativePath = docRel,
                SourceDiskPath = disk,
                OutputPath = outputPath,
                Document = document,
                Title = MarkdownText.FindTitle(document) ?? Path.GetFileNameWithoutExtension(docRel),
            };
            pages.Add(page);
        }
        return pages.OrderBy(p => p.OutputPath, StringComparer.Ordinal).ToList();
    }

    private NavNode BuildSourceNav(DocSource source, List<Page> sourcePages, Dictionary<string, Page> pagesByDisk)
    {
        var navDisk = Path.Combine(source.DocsRootDisk, source.NavFile.Replace('/', Path.DirectorySeparatorChar));
        NavNode nav;
        if (pagesByDisk.TryGetValue(NormalizeDisk(navDisk), out var navPage))
        {
            nav = NavTreeBuilder.BuildFromIndexPage(navPage.Document);
            ResolveNavHrefs(nav, navPage, pagesByDisk);
        }
        else
        {
            nav = new NavNode { Title = "" };
        }
        AppendOrphanedPages(nav, sourcePages);
        return nav;
    }

    private void ResolveNavHrefs(NavNode node, Page navPage, Dictionary<string, Page> pagesByDisk)
    {
        if (node.Href != null)
        {
            var split = RepoPaths.SplitRelativeHref(node.Href);
            if (split != null)
            {
                var target = ResolvePageLink(navPage, split.Value.PathPart, pagesByDisk);
                node.OutputHref = target?.OutputPath ?? ResolveDemoAsset(navPage, split.Value.PathPart);
            }
        }
        foreach (var child in node.Children)
        {
            ResolveNavHrefs(child, navPage, pagesByDisk);
        }
    }

    /// <summary>
    /// Every discovered page in a source not already reachable from that source's nav tree still gets
    /// a sidebar entry (under a synthesized "More" group), so nothing is silently unreachable even if
    /// the source's index.md hasn't been updated to link it yet.
    /// </summary>
    private static void AppendOrphanedPages(NavNode nav, List<Page> sourcePages)
    {
        var linked = new HashSet<string?>();
        CollectOutputHrefs(nav, linked);

        var navFileHome = sourcePages
            .FirstOrDefault(p => string.Equals(
                Path.GetFileName(p.DocRelativePath), p.Source.NavFile, StringComparison.Ordinal));

        var orphans = sourcePages
            .Where(p => p != navFileHome)
            .Where(p => !linked.Contains(p.OutputPath))
            .OrderBy(p => p.OutputPath, StringComparer.Ordinal)
            .ToList();

        if (orphans.Count == 0) return;

        var more = new NavNode { Title = "More" };
        foreach (var orphan in orphans)
        {
            more.Children.Add(new NavNode { Title = orphan.Title, OutputHref = orphan.OutputPath });
        }
        nav.Children.Add(more);
    }

    private static void CollectOutputHrefs(NavNode node, HashSet<string?> into)
    {
        if (node.OutputHref != null) into.Add(node.OutputHref);
        foreach (var child in node.Children) CollectOutputHrefs(child, into);
    }

    private void RewriteLinks(Page page, Dictionary<string, Page> pagesByDisk, Dictionary<string, string> assetsToCopy)
    {
        foreach (var link in page.Document.Descendants<LinkInline>().ToList())
        {
            var split = RepoPaths.SplitRelativeHref(link.Url ?? "");
            if (split == null) continue;
            var (pathPart, suffix) = split.Value;

            var target = ResolvePageLink(page, pathPart, pagesByDisk);
            if (target != null)
            {
                link.Url = RepoPaths.RelativeHref(page.OutputPath, target.OutputPath) + suffix;
                continue;
            }

            var asset = ResolveAsset(page, pathPart);
            if (asset != null)
            {
                assetsToCopy[asset.Value.OutputPath] = asset.Value.DiskPath;
                link.Url = RepoPaths.RelativeHref(page.OutputPath, asset.Value.OutputPath) + suffix;
                continue;
            }

            var demoPath = ResolveDemoAsset(page, pathPart);
            if (demoPath != null)
            {
                link.Url = RepoPaths.RelativeHref(page.OutputPath, demoPath) + suffix;
                continue;
            }

            Console.WriteLine($"warning: unresolved link '{link.Url}' in {page.OutputPath}");
        }
    }

    /// <summary>Resolves a doc-relative href to the page it points at, by absolute disk path (cross-source safe).</summary>
    private static Page? ResolvePageLink(Page fromPage, string hrefPathPart, Dictionary<string, Page> pagesByDisk)
    {
        var baseDir = Path.GetDirectoryName(fromPage.SourceDiskPath)!;
        var candidate = NormalizeDisk(Path.GetFullPath(Path.Combine(baseDir, hrefPathPart.Replace('/', Path.DirectorySeparatorChar))));
        // A doc slug can contain a dot (a version like "1.0"), so try both with and without .md.
        if (pagesByDisk.TryGetValue(candidate, out var exact)) return exact;
        if (pagesByDisk.TryGetValue(candidate + ".md", out var withExt)) return withExt;
        return null;
    }

    // Only genuine web assets (images embedded in a doc page) get vendored into the site. Docs link to
    // plenty of other real repo files too (SAM templates, docker-compose.yaml, test .cs files) - those
    // aren't published, so they're left as unresolved relative links.
    private static readonly HashSet<string> WebAssetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".ico", ".pdf",
    };

    /// <summary>An image asset that lives inside the page's own source docs root gets copied under that source's prefix.</summary>
    private static (string OutputPath, string DiskPath)? ResolveAsset(Page fromPage, string hrefPathPart)
    {
        if (!WebAssetExtensions.Contains(Path.GetExtension(hrefPathPart))) return null;
        var baseDir = Path.GetDirectoryName(fromPage.SourceDiskPath)!;
        var diskFull = Path.GetFullPath(Path.Combine(baseDir, hrefPathPart.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(diskFull)) return null;

        var rootFull = Path.GetFullPath(fromPage.Source.DocsRootDisk);
        var relFromRoot = Path.GetRelativePath(rootFull, diskFull);
        if (relFromRoot.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relFromRoot))
        {
            // Asset escapes the source docs root - leave it as an unresolved relative link.
            return null;
        }
        var outputPath = $"{fromPage.Source.UrlPrefix}/{RepoPaths.Normalize(relFromRoot)}";
        return (outputPath, diskFull);
    }

    // website/demos/** are pre-built, self-contained static pages copied verbatim by CopyDemos() and
    // published at the site root as "demos/...", not crawled/rendered as docs pages. Docs reference
    // them by a relative path that resolves to a ".../demos/<rest>" tail (the demos live in benzene's
    // website/demos/ regardless of which repo the referencing docs came from).
    private string? ResolveDemoAsset(Page fromPage, string hrefPathPart)
    {
        var normalized = RepoPaths.Normalize(hrefPathPart);
        const string marker = "demos/";
        var idx = normalized.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return null;
        var rest = normalized[(idx + marker.Length)..];
        if (rest.Length == 0) return null;
        var disk = Path.Combine(_repoRoot, "website", "demos", rest.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(disk) ? $"demos/{rest}" : null;
    }

    private string RenderPage(Page page, NavNode nav, List<DocSource> languages)
    {
        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        _pipeline.Setup(renderer);
        renderer.Render(page.Document);
        var bodyHtml = writer.ToString();

        return Layout.RenderDocsPage(page.Title, bodyHtml, nav, page.OutputPath, page.Source, languages);
    }

    private void WriteOutput(string outputPath, string html)
    {
        var diskPath = RepoPaths.ToDiskPath(_outDir, outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(diskPath)!);
        File.WriteAllText(diskPath, html);
    }

    private void CopyStaticAssets(Dictionary<string, string> crawledAssets)
    {
        var assetsSourceDir = Path.Combine(AppContext.BaseDirectory, "assets");
        if (!Directory.Exists(assetsSourceDir))
        {
            assetsSourceDir = Path.Combine(_repoRoot, "website", "generator", "assets");
        }

        foreach (var file in Directory.EnumerateFiles(assetsSourceDir))
        {
            File.Copy(file, Path.Combine(_outDir, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var (outputPath, diskPath) in crawledAssets)
        {
            var dst = RepoPaths.ToDiskPath(_outDir, outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.Copy(diskPath, dst, overwrite: true);
        }
    }

    private void CopyDemos()
    {
        var demosSourceDir = Path.Combine(_repoRoot, "website", "demos");
        if (!Directory.Exists(demosSourceDir)) return;

        var demosOutDir = Path.Combine(_outDir, "demos");
        foreach (var file in Directory.EnumerateFiles(demosSourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(demosSourceDir, file);
            var dst = Path.Combine(demosOutDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.Copy(file, dst, overwrite: true);
        }
    }

    private List<(string File, string Href)> SelfCheck(IEnumerable<string> outputPaths)
    {
        var broken = new List<(string, string)>();
        foreach (var outputPath in outputPaths)
        {
            var disk = RepoPaths.ToDiskPath(_outDir, outputPath);
            var html = File.ReadAllText(disk);
            foreach (System.Text.RegularExpressions.Match match in
                     System.Text.RegularExpressions.Regex.Matches(html, "href=\"([^\"#]+\\.html)(#[^\"]*)?\""))
            {
                var href = match.Groups[1].Value;
                if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var targetOutputPath = RepoPaths.CombineRepoRelative(outputPath, href);
                if (!File.Exists(RepoPaths.ToDiskPath(_outDir, targetOutputPath)))
                {
                    broken.Add((outputPath, href));
                }
            }
        }
        return broken;
    }

    private static string NormalizeDisk(string path) => Path.GetFullPath(path);
}
