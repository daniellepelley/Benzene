namespace Benzene.Website.Generator;

/// <summary>
/// D2 from work/website-information-architecture-strategy.md §6.5: the picture a cold-developer
/// walkthrough asked for by name and the site never drew - "the comparison I arrived making" -
/// requiring no Benzene vocabulary to read. Left panel: three separately built, separately deployed
/// services each re-implementing the same cross-cutting concerns. Right panel: one service, one
/// shared pipeline, three bindings. Same grammar as <see cref="TransportDiagram"/>: muted = your own
/// code (including the duplicated boxes - duplication is the problem, not a Benzene/non-Benzene
/// split), accent = the Benzene-owned pipeline, arrows always left-to-right into the box they feed.
/// </summary>
internal static class BeforeAfterDiagram
{
    private sealed record DuplicatedBox(double Y, string Label);
    private sealed record Entry(double Y, string Label);

    private static readonly DuplicatedBox[] Without =
    [
        new(60, "HTTP service"),
        new(164, "SQS consumer"),
        new(268, "Kafka consumer"),
    ];

    private static readonly Entry[] Entries =
    [
        new(175, "HTTP"),
        new(230, "SQS"),
        new(285, "Kafka"),
    ];

    private const double BoxX = 40, BoxW = 320, BoxH = 84;
    private const double AccentX = 460, AccentW = 320, AccentY = 90, AccentH = 230;

    public static string Render()
    {
        var boxes = string.Join("\n", Without.Select(b => $"""
            <rect class="ba-box" x="{BoxX}" y="{b.Y}" width="{BoxW}" height="{BoxH}" rx="10"/>
            <text class="ba-box-label" x="{BoxX + BoxW / 2}" y="{b.Y + BoxH / 2 - 6:F1}" text-anchor="middle">{Html(b.Label)}</text>
            <text class="ba-box-sublabel" x="{BoxX + BoxW / 2}" y="{b.Y + BoxH / 2 + 16:F1}" text-anchor="middle">validation &middot; logging &middot; auth</text>
            """));

        var entries = string.Join("\n", Entries.Select(e => $"""
            <text class="ba-entry-label" x="{AccentX - 55}" y="{e.Y + 4:F1}" text-anchor="end">{Html(e.Label)}</text>
            <line class="ba-line" x1="{AccentX - 50}" y1="{e.Y}" x2="{AccentX}" y2="{e.Y}" marker-end="url(#ba-arrow)"/>
            """));

        return $"""
            <svg class="before-after-diagram" viewBox="0 0 820 400" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="Without Benzene: an HTTP service, an SQS consumer, and a Kafka consumer are three separate deployables, each re-implementing validation, logging, and auth. With Benzene: HTTP, SQS, and Kafka all enter one service, sharing one middleware pipeline for validation, logging, and auth, written once.">
              <defs>
                <marker id="ba-arrow" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
                  <path class="ba-arrow-head" d="M0,0 L10,5 L0,10 z"/>
                </marker>
              </defs>
              <text class="ba-heading" x="{BoxX + BoxW / 2}" y="30" text-anchor="middle">Without Benzene</text>
              {boxes}
              <text class="ba-caption" x="{BoxX + BoxW / 2}" y="368" text-anchor="middle">Same concerns, written three times.</text>
              <text class="ba-caption" x="{BoxX + BoxW / 2}" y="386" text-anchor="middle">Deployed three times.</text>

              <text class="ba-heading" x="{AccentX + AccentW / 2}" y="30" text-anchor="middle">With Benzene</text>
              {entries}
              <rect class="ba-accent" x="{AccentX}" y="{AccentY}" width="{AccentW}" height="{AccentH}" rx="14"/>
              <text class="ba-accent-label" x="{AccentX + AccentW / 2}" y="{AccentY + 30}" text-anchor="middle">One service</text>
              <text class="ba-accent-sublabel" x="{AccentX + AccentW / 2}" y="{AccentY + 52}" text-anchor="middle">PlaceOrderHandler</text>
              <text class="ba-accent-sublabel" x="{AccentX + AccentW / 2}" y="{AccentY + AccentH - 20:F1}" text-anchor="middle">validation &middot; logging &middot; auth &mdash; once</text>
              <text class="ba-caption" x="{AccentX + AccentW / 2}" y="368" text-anchor="middle">One service. One pipeline.</text>
              <text class="ba-caption" x="{AccentX + AccentW / 2}" y="386" text-anchor="middle">Three bindings.</text>
            </svg>
            """;
    }

    private static string Html(string s) => System.Net.WebUtility.HtmlEncode(s);
}
