namespace Benzene.Website.Generator;

/// <summary>
/// Repo-relative paths are represented as forward-slash strings (e.g. "docs/getting-started.md")
/// regardless of host OS, so they can be used directly as URL segments and dictionary keys.
/// </summary>
internal static class RepoPaths
{
    public static string Normalize(string path) => path.Replace('\\', '/');

    public static string ToDiskPath(string repoRoot, string repoRelativePath) =>
        Path.Combine(repoRoot, repoRelativePath.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// Resolves a markdown link's href (which may include an anchor/query) against the directory
    /// of the page containing it, returning a normalized repo-relative path with no leading "./"
    /// or anchor/query, plus the anchor (if any) preserved separately. Returns null for absolute
    /// URLs, mailto links, or anchor-only links, since those are never rewritten.
    /// </summary>
    public static (string PathPart, string Suffix)? SplitRelativeHref(string href)
    {
        if (string.IsNullOrWhiteSpace(href)) return null;
        if (href.StartsWith('#')) return null;
        if (Uri.TryCreate(href, UriKind.Absolute, out _)) return null;
        if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) return null;

        var anchorIndex = href.IndexOf('#');
        var queryIndex = href.IndexOf('?');
        var cut = href.Length;
        if (anchorIndex >= 0) cut = Math.Min(cut, anchorIndex);
        if (queryIndex >= 0) cut = Math.Min(cut, queryIndex);
        var pathPart = href[..cut];
        var suffix = href[cut..];
        return string.IsNullOrEmpty(pathPart) ? null : (pathPart, suffix);
    }

    /// <summary>Combines a page's repo-relative directory with a relative href and normalizes "..".</summary>
    public static string CombineRepoRelative(string currentPageRepoRelativePath, string relativeHref)
    {
        var currentDir = System.IO.Path.GetDirectoryName(Normalize(currentPageRepoRelativePath)) ?? "";
        var combined = currentDir.Length == 0 ? relativeHref : $"{currentDir}/{relativeHref}";

        var segments = new List<string>();
        foreach (var segment in Normalize(combined).Split('/'))
        {
            if (segment is "" or ".") continue;
            if (segment == ".." && segments.Count > 0) segments.RemoveAt(segments.Count - 1);
            else segments.Add(segment);
        }
        return string.Join('/', segments);
    }

    /// <summary>
    /// Resolves a doc-relative link that points at a repo file (not a published page) to its URL in the
    /// source repo's blob view. <paramref name="blobDocsRoot"/> is the blob URL of the source's docs
    /// root; the href is resolved relative to the page's own location under that root, so a link that
    /// climbs out of the docs tree (e.g. <c>../examples/…</c>) lands on the right repo path and any
    /// <c>..</c> is normalized away instead of being left in the emitted URL.
    /// </summary>
    public static string RepoBlobHref(string blobDocsRoot, string currentPageDocRelativePath, string relativeHref)
    {
        var docsRoot = new Uri($"{blobDocsRoot.TrimEnd('/')}/");            // treat the docs root as a directory
        var pageDir = System.IO.Path.GetDirectoryName(Normalize(currentPageDocRelativePath)) ?? "";
        var basis = pageDir.Length == 0 ? docsRoot : new Uri(docsRoot, $"{pageDir}/");
        return new Uri(basis, relativeHref).AbsoluteUri;
    }

    /// <summary>Relative URL from one output HTML file to another, both given as repo-root-relative output paths.</summary>
    public static string RelativeHref(string fromOutputPath, string toOutputPath)
    {
        var fromDirName = System.IO.Path.GetDirectoryName(Normalize(fromOutputPath)) ?? "";
        var fromDirSegments = fromDirName.Length == 0 ? Array.Empty<string>() : fromDirName.Split('/');

        var toNormalized = Normalize(toOutputPath);
        var toFileName = toNormalized[(toNormalized.LastIndexOf('/') + 1)..];
        var toDirName = System.IO.Path.GetDirectoryName(toNormalized) ?? "";
        var toDirSegments = toDirName.Length == 0 ? Array.Empty<string>() : toDirName.Split('/');

        var common = 0;
        while (common < fromDirSegments.Length && common < toDirSegments.Length && fromDirSegments[common] == toDirSegments[common])
        {
            common++;
        }

        var parts = new List<string>();
        for (var i = common; i < fromDirSegments.Length; i++) parts.Add("..");
        for (var i = common; i < toDirSegments.Length; i++) parts.Add(toDirSegments[i]);
        parts.Add(toFileName);
        return string.Join('/', parts);
    }
}
