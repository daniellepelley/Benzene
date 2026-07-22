using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Benzene.Website.Generator;

/// <summary>
/// One entry in the docs sidebar. A node with <see cref="Href"/> is a clickable page link; a node
/// with only a <see cref="Title"/> is a non-clickable group header with <see cref="Children"/>.
/// </summary>
internal sealed class NavNode
{
    public string Title { get; init; } = "";
    public string? Href { get; init; }
    public string? OutputHref { get; set; }
    public List<NavNode> Children { get; } = new();
}

internal static class NavTreeBuilder
{
    /// <summary>
    /// Builds the docs sidebar from docs/index.md's own nested bullet list (the first top-level
    /// list in the document) - so index.md stays the single source of truth for both the docs
    /// home page content and the site navigation.
    /// </summary>
    public static NavNode BuildFromIndexPage(MarkdownDocument indexDocument)
    {
        var topLevelList = indexDocument.OfType<ListBlock>().FirstOrDefault();
        var root = new NavNode { Title = "" };
        if (topLevelList != null)
        {
            root.Children.AddRange(BuildFromList(topLevelList));
        }
        return root;
    }

    private static List<NavNode> BuildFromList(ListBlock list)
    {
        var nodes = new List<NavNode>();
        foreach (var itemObj in list)
        {
            if (itemObj is not ListItemBlock item) continue;

            NavNode? node = null;
            ListBlock? nested = null;
            foreach (var child in item)
            {
                switch (child)
                {
                    case ParagraphBlock paragraph:
                        node = ExtractNode(paragraph.Inline);
                        break;
                    case ListBlock nestedList:
                        nested = nestedList;
                        break;
                }
            }

            if (node == null) continue;
            if (nested != null)
            {
                node.Children.AddRange(BuildFromList(nested));
            }
            nodes.Add(node);
        }
        return nodes;
    }

    private static NavNode? ExtractNode(ContainerInline? inline)
    {
        if (inline == null) return null;

        var link = inline.Descendants<LinkInline>().FirstOrDefault(l => !l.IsImage);
        if (link != null)
        {
            return new NavNode { Title = MarkdownText.GetPlainText(link).Trim(), Href = link.Url };
        }

        // A group header is conventionally "- **Title**", optionally followed by descriptive prose
        // ("- **Title** — why this section exists"). The prose belongs on the docs home page, not
        // in the sidebar - so when the bullet carries a bold run, that alone is the title.
        var strong = inline.Descendants<EmphasisInline>().FirstOrDefault(x => x.DelimiterCount == 2);
        var text = (strong != null ? MarkdownText.GetPlainText(strong) : MarkdownText.GetPlainText(inline)).Trim();
        return text.Length == 0 ? null : new NavNode { Title = text };
    }
}
