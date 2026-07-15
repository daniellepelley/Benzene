namespace Benzene.Website.Generator;

/// <summary>
/// Benzene's mark: a hexagon with an inscribed ring, the standard chemistry shorthand for an
/// aromatic ring (the delocalized electrons in the real benzene molecule, C6H6) - the same shape
/// as "hexagonal architecture", so it reads correctly on both counts. Uses currentColor so it
/// inherits whatever color the surrounding CSS sets (header wordmark vs. hero, light vs. dark).
/// </summary>
internal static class Logo
{
    public static string Inline(int size) => $"""
        <svg class="logo-mark" viewBox="0 0 100 100" width="{size}" height="{size}" aria-hidden="true">
          <polygon points="50,4 93,27 93,73 50,96 7,73 7,27" fill="none" stroke="currentColor" stroke-width="6"/>
          <circle cx="50" cy="50" r="26" fill="none" stroke="currentColor" stroke-width="6"/>
        </svg>
        """;
}
