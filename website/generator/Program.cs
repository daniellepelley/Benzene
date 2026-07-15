using Benzene.Website.Generator;

var repoRoot = Directory.GetCurrentDirectory();
var outDir = "website/dist";

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
}

if (!Path.IsPathRooted(outDir))
{
    outDir = Path.Combine(repoRoot, outDir);
}

if (!File.Exists(Path.Combine(repoRoot, "README.md")) || !Directory.Exists(Path.Combine(repoRoot, "docs")))
{
    Console.Error.WriteLine(
        $"error: '{repoRoot}' doesn't look like the Benzene repo root (no README.md/docs found). " +
        "Run this from the repo root, or pass --repo-root <path>.");
    return 1;
}

return new SiteBuilder(repoRoot, outDir).Run();
