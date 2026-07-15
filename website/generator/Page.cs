using Markdig.Syntax;

namespace Benzene.Website.Generator;

/// <summary>One markdown source file that becomes one output HTML page.</summary>
internal sealed class Page
{
    public required string SourcePath { get; init; }
    public required string OutputPath { get; init; }
    public required MarkdownDocument Document { get; init; }
    public string Title { get; set; } = "";
}
