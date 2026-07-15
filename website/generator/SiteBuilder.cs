using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Benzene.Website.Generator;

internal sealed class SiteBuilder
{
    // docs/specification/conformance/*.json fixtures are already excluded because discovery only
    // picks up *.md files - conformance/README.md itself is genuine prose, reachable from
    // docs/specification/README.md, and stays included.
    private static readonly string[] ExcludedDirPrefixes =
    [
        "docs/plans/",
    ];

    private static readonly HashSet<string> ExcludedFiles = new(StringComparer.Ordinal)
    {
        "docs/DOCUMENTATION_QUICK_REFERENCE.md",
    };

    private readonly string _repoRoot;
    private readonly string _outDir;
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public SiteBuilder(string repoRoot, string outDir)
    {
        _repoRoot = repoRoot;
        _outDir = outDir;
    }

    public int Run()
    {
        var sourcePaths = DiscoverSourcePaths();
        var pages = new Dictionary<string, Page>(StringComparer.Ordinal);

        foreach (var sourcePath in sourcePaths)
        {
            var text = File.ReadAllText(RepoPaths.ToDiskPath(_repoRoot, sourcePath));
            var document = Markdown.Parse(text, _pipeline);
            var outputPath = ComputeOutputPath(sourcePath);
            var page = new Page { SourcePath = sourcePath, OutputPath = outputPath, Document = document };
            page.Title = MarkdownText.FindTitle(document) ?? Path.GetFileNameWithoutExtension(sourcePath);
            pages[sourcePath] = page;
        }

        if (!pages.TryGetValue("docs/index.md", out var docsIndexPage))
        {
            throw new InvalidOperationException("docs/index.md is required as the docs nav source but was not found.");
        }

        var nav = NavTreeBuilder.BuildFromIndexPage(docsIndexPage.Document);
        ResolveNavHrefs(nav, "docs/index.md", pages);
        AppendOrphanedDocsPages(nav, pages);

        var assetsToCopy = new HashSet<string>(StringComparer.Ordinal);
        foreach (var page in pages.Values)
        {
            RewriteLinks(page, pages, assetsToCopy);
        }

        Directory.CreateDirectory(_outDir);
        foreach (var page in pages.Values)
        {
            var html = RenderPage(page, nav);
            var diskPath = RepoPaths.ToDiskPath(_outDir, page.OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(diskPath)!);
            File.WriteAllText(diskPath, html);
        }

        CopyStaticAssets(assetsToCopy);

        var brokenLinks = SelfCheck(pages.Values.Select(p => p.OutputPath));
        if (brokenLinks.Count > 0)
        {
            Console.Error.WriteLine($"Generation failed: {brokenLinks.Count} broken internal link(s):");
            foreach (var (file, href) in brokenLinks)
            {
                Console.Error.WriteLine($"  {file} -> {href}");
            }
            return 1;
        }

        Console.WriteLine($"Generated {pages.Count} pages to {_outDir}");
        return 0;
    }

    private List<string> DiscoverSourcePaths()
    {
        var docsRootDisk = Path.Combine(_repoRoot, "docs");
        var mdFiles = Directory.EnumerateFiles(docsRootDisk, "*.md", SearchOption.AllDirectories)
            .Select(p => RepoPaths.Normalize(Path.GetRelativePath(_repoRoot, p)))
            .Where(rel => !ExcludedFiles.Contains(rel) && !ExcludedDirPrefixes.Any(rel.StartsWith))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        mdFiles.Add("README.md");
        return mdFiles;
    }

    private static string ComputeOutputPath(string sourcePath) =>
        sourcePath == "README.md" ? "index.html" : sourcePath[..^".md".Length] + ".html";

    private static void ResolveNavHrefs(NavNode node, string navSourcePath, Dictionary<string, Page> pages)
    {
        if (node.Href != null)
        {
            var target = ResolveLinkTarget(navSourcePath, node.Href, pages);
            node.OutputHref = target?.OutputPath;
        }
        foreach (var child in node.Children)
        {
            ResolveNavHrefs(child, navSourcePath, pages);
        }
    }

    /// <summary>
    /// Every crawled docs page not already reachable from docs/index.md's nav tree still gets a
    /// sidebar entry (under a synthesized "More" group) so nothing under docs/ is ever silently
    /// unreachable from the site, even if index.md hasn't been updated to link it yet.
    /// </summary>
    private static void AppendOrphanedDocsPages(NavNode nav, Dictionary<string, Page> pages)
    {
        var linked = new HashSet<string?>();
        CollectOutputHrefs(nav, linked);

        var orphans = pages.Values
            .Where(p => p.SourcePath.StartsWith("docs/", StringComparison.Ordinal) && p.SourcePath != "docs/index.md")
            .Where(p => !linked.Contains(p.OutputPath))
            .OrderBy(p => p.SourcePath, StringComparer.Ordinal)
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

    private void RewriteLinks(Page page, Dictionary<string, Page> pages, HashSet<string> assetsToCopy)
    {
        foreach (var link in page.Document.Descendants<LinkInline>().ToList())
        {
            var split = RepoPaths.SplitRelativeHref(link.Url ?? "");
            if (split == null) continue;
            var (pathPart, suffix) = split.Value;

            var target = ResolveLinkTarget(page.SourcePath, pathPart, pages);
            if (target != null)
            {
                link.Url = RepoPaths.RelativeHref(page.OutputPath, target.OutputPath) + suffix;
                continue;
            }

            var assetPath = ResolveAsset(page.SourcePath, pathPart);
            if (assetPath != null)
            {
                assetsToCopy.Add(assetPath);
                link.Url = RepoPaths.RelativeHref(page.OutputPath, assetPath) + suffix;
                continue;
            }

            Console.WriteLine($"warning: unresolved link '{link.Url}' in {page.SourcePath}");
        }
    }

    private static Page? ResolveLinkTarget(string currentSourcePath, string hrefPathPart, Dictionary<string, Page> pages)
    {
        // Some doc slugs contain dots (e.g. "migration-alpha-to-1.0"), so Path.HasExtension can't
        // reliably tell whether ".md" is already present - just try both forms unconditionally.
        var combined = RepoPaths.CombineRepoRelative(currentSourcePath, hrefPathPart);
        if (pages.TryGetValue(combined, out var exact)) return exact;
        if (pages.TryGetValue(combined + ".md", out var withExt)) return withExt;
        return null;
    }

    // Only genuine web assets (images embedded in a doc page) get vendored into the site.
    // Docs link to plenty of other real repo files too (SAM templates, docker-compose.yaml, test
    // .cs files) - those aren't meant to be published as part of the static site, so they're left
    // as unresolved relative links (same as the pre-existing "../examples" directory links).
    private static readonly HashSet<string> WebAssetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".ico", ".pdf",
    };

    private string? ResolveAsset(string currentSourcePath, string hrefPathPart)
    {
        var combined = RepoPaths.CombineRepoRelative(currentSourcePath, hrefPathPart);
        if (!WebAssetExtensions.Contains(Path.GetExtension(combined))) return null;
        var disk = RepoPaths.ToDiskPath(_repoRoot, combined);
        return File.Exists(disk) ? combined : null;
    }

    private string RenderPage(Page page, NavNode nav)
    {
        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        _pipeline.Setup(renderer);
        renderer.Render(page.Document);
        var bodyHtml = writer.ToString();

        return page.SourcePath == "README.md"
            ? Layout.RenderMarketingPage(page.Title, bodyHtml, page.OutputPath)
            : Layout.RenderDocsPage(page.Title, bodyHtml, nav, page.OutputPath);
    }

    private void CopyStaticAssets(HashSet<string> crawledAssets)
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

        foreach (var asset in crawledAssets)
        {
            var src = RepoPaths.ToDiskPath(_repoRoot, asset);
            var dst = RepoPaths.ToDiskPath(_outDir, asset);
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.Copy(src, dst, overwrite: true);
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
}
