namespace Benzene.Website.Generator;

/// <summary>
/// One documentation source rendered into the site. After the repo split the site is no longer a
/// single <c>docs/</c> tree: it stitches together several sources — each language port's docs (each
/// checked out from its own repo by CI) plus the cross-language specification. Every source has its
/// own docs root on disk and its own URL prefix, so the same site can render <c>/dotnet/docs/…</c>,
/// a future <c>/typescript/docs/…</c>, and the spec at <c>/docs/specification/…</c> side by side.
/// </summary>
internal sealed class DocSource
{
    /// <summary>Stable id, e.g. "dotnet", "spec". Used for the language dropdown and CLI wiring.</summary>
    public required string Id { get; init; }

    /// <summary>Human label shown in the language dropdown / section headings, e.g. ".NET".</summary>
    public required string Label { get; init; }

    /// <summary>
    /// Output path prefix every page from this source is rendered under (forward-slash, repo-root
    /// relative), e.g. "dotnet/docs" → dotnet/docs/getting-started.html, or "docs/specification".
    /// </summary>
    public required string UrlPrefix { get; init; }

    /// <summary>Absolute path to this source's docs root directory on disk.</summary>
    public required string DocsRootDisk { get; init; }

    /// <summary>
    /// The nav source file within the docs root (its nested bullet list drives this source's
    /// sidebar). Defaults to index.md; a source without one (e.g. the spec) can point at README.md.
    /// </summary>
    public string NavFile { get; init; } = "index.md";

    /// <summary>True for a language port (appears in the language dropdown); false for the spec.</summary>
    public bool IsLanguage { get; init; }

    /// <summary>
    /// The prefix these pages used to live under before the split, if any (e.g. the .NET docs were at
    /// "docs/…" and are now at "dotnet/docs/…"). When set and different from <see cref="UrlPrefix"/>,
    /// the generator emits a redirect stub at each legacy path so existing inbound links survive. A
    /// legacy path that collides with a real emitted page (the hub, a spec page) is skipped.
    /// </summary>
    public string? LegacyUrlPrefix { get; init; }

    /// <summary>
    /// Subdirectory prefixes (relative to the docs root, forward-slash, trailing slash) whose *.md
    /// files this source does not own — e.g. the .NET source excludes "specification/" because the
    /// spec is its own source, and "plans/" (internal roadmap docs).
    /// </summary>
    public string[] ExcludedSubdirs { get; init; } = [];

    /// <summary>File names (relative to the docs root) to exclude, e.g. a contributor cheat-sheet.</summary>
    public string[] ExcludedFiles { get; init; } = [];

    /// <summary>Output path of this source's docs home (its nav file rendered), e.g. "dotnet/docs/index.html".</summary>
    public string HomeOutputPath =>
        $"{UrlPrefix}/{System.IO.Path.GetFileNameWithoutExtension(NavFile)}.html";
}
