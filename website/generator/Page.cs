using Markdig.Syntax;

namespace Benzene.Website.Generator;

/// <summary>One markdown source file that becomes one output HTML page.</summary>
internal sealed class Page
{
    /// <summary>The doc source this page belongs to (language port, or the spec).</summary>
    public required DocSource Source { get; init; }

    /// <summary>Path of the markdown file relative to its source's docs root, e.g. "getting-started.md".</summary>
    public required string DocRelativePath { get; init; }

    /// <summary>Absolute path of the markdown file on disk.</summary>
    public required string SourceDiskPath { get; init; }

    /// <summary>Output path relative to the site root, e.g. "dotnet/docs/getting-started.html".</summary>
    public required string OutputPath { get; init; }

    public required MarkdownDocument Document { get; init; }
    public string Title { get; set; } = "";

    /// <summary>Meta-description for this page (first paragraph); empty falls back to a generic one.</summary>
    public string Description { get; set; } = "";
}
