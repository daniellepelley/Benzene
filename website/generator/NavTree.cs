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

        // A group header is conventionally "- **Title**", optionally followed by descriptive prose
        // ("- **Title** — why this section exists"). The prose belongs on the docs home page, not in
        // the sidebar - so when the bullet carries a bold run, that alone is the title.
        //
        // The bold run has to be looked for BEFORE the link, and it only counts when it sits outside
        // one. That prose regularly contains links, and taking the first link in the bullet instead
        // renamed the whole section after whatever it happened to mention: the .NET docs' "**Benzene
        // Specification (Draft)** — ... lives in the [`benzene`](...) repo" came out as a section
        // called "benzene". A bullet written "- [**Title**](page.md)" is still a page link, which is
        // why the bold run must not be inside the link to win.
        var link = inline.Descendants<LinkInline>().FirstOrDefault(l => !l.IsImage);
        var strong = inline.Descendants<EmphasisInline>()
            .FirstOrDefault(x => x.DelimiterCount == 2 && !IsInside(x, link));

        if (strong == null && link != null)
        {
            return new NavNode { Title = MarkdownText.GetPlainText(link).Trim(), Href = link.Url };
        }

        var text = (strong != null ? MarkdownText.GetPlainText(strong) : MarkdownText.GetPlainText(inline)).Trim();
        return text.Length == 0 ? null : new NavNode { Title = text };
    }

    private static bool IsInside(Inline node, LinkInline? link)
    {
        if (link == null) return false;

        for (var parent = node.Parent; parent != null; parent = parent.Parent)
        {
            if (ReferenceEquals(parent, link)) return true;
        }

        return false;
    }
}
