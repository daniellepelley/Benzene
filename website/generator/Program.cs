using Benzene.Website.Generator;

var repoRoot = Directory.GetCurrentDirectory();
var outDir = "website/dist";
string? dotnetDocs = null;               // override: the benzene-dotnet checkout's docs root (CI)
var extraSources = new List<DocSource>(); // future languages via --source id=label=urlPrefix=path[=navFile]

for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--out" && i + 1 < args.Length)
    {
        outDir = args[++i];
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
        // id=Label=urlPrefix=docsRootPath[=navFile]  (a language port; appears in the switcher)
        var parts = args[++i].Split('=');
        if (parts.Length < 4)
        {
            Console.Error.WriteLine("error: --source expects id=Label=urlPrefix=docsRootPath[=navFile]");
            return 1;
        }
        extraSources.Add(new DocSource
        {
            Id = parts[0],
            Label = parts[1],
            UrlPrefix = parts[2],
            DocsRootDisk = Path.GetFullPath(parts[3]),
            NavFile = parts.Length >= 5 ? parts[4] : "index.md",
            IsLanguage = true,
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
    ExcludedSubdirs = ["specification/", "plans/"],
    ExcludedFiles = ["DOCUMENTATION_QUICK_REFERENCE.md"],
};

// The cross-language spec source (stays in benzene). Not a language: it does not appear in the
// language switcher; it is the headline the docs hub leads with.
var specSource = new DocSource
{
    Id = "spec",
    Label = "Specification",
    UrlPrefix = "docs/specification",
    DocsRootDisk = specRoot,
    NavFile = "README.md",
    IsLanguage = false,
};

var sources = new List<DocSource> { specSource, dotnetSource };
sources.AddRange(extraSources);

return new SiteBuilder(repoRoot, outDir, sources).Run();
