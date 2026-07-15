using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Benzene.Website.Generator;

internal static class MarkdownText
{
    /// <summary>Flattens an inline tree (bold, links, plain text, ...) to plain text.</summary>
    public static string GetPlainText(Inline? inline)
    {
        if (inline == null) return "";
        var text = new System.Text.StringBuilder();
        Append(inline, text);
        return text.ToString();
    }

    private static void Append(Inline inline, System.Text.StringBuilder text)
    {
        switch (inline)
        {
            case LiteralInline literal:
                text.Append(literal.Content.ToString());
                break;
            case LineBreakInline:
                text.Append(' ');
                break;
            case ContainerInline container:
                foreach (var child in container)
                {
                    Append(child, text);
                }
                break;
            case CodeInline code:
                text.Append(code.Content);
                break;
        }
    }

    /// <summary>The text of the first level-1 heading in the document, or null if there isn't one.</summary>
    public static string? FindTitle(MarkdownDocument document)
    {
        var heading = document.Descendants<HeadingBlock>().FirstOrDefault(h => h.Level == 1);
        return heading == null ? null : GetPlainText(heading.Inline).Trim();
    }
}
