namespace Benzene.Website.Generator;

/// <summary>
/// D1 from work/website-information-architecture-strategy.md §6.5: three different transports
/// converging on one handler. <see cref="ArchitectureDiagram"/> answers "where does it run" (hosts,
/// arranged around a hexagon); this answers the actual pitch both cold-developer walkthroughs asked
/// for by name - "how many things can call the same handler" - with a concrete worked example
/// rather than placeholder labels, per the visual grammar in §6.4: shape carries role (pill =
/// transport, accent rect = the Benzene-owned handler, muted rounded rect = your own result type),
/// flow is strictly left-to-right with arrowheads, colour never decorates.
/// <para>
/// Placed directly under the landing page's "Get started" code panel, captioned as that code
/// "drawn out" - so this uses the <em>same</em> example the panel does (<c>greet</c> /
/// <c>GreetHandler</c>, <see cref="MarketingContent.Languages"/>'s .NET snippet), not an
/// independent one. A first pass used <c>order:placed</c> here instead; a cold-developer
/// walkthrough caught the mismatch immediately ("the diagram switches examples without saying so,
/// right when it claims to be 'the code above, drawn out'") and it cost real trust. Keep these in
/// sync if that snippet's topic name ever changes.
/// </para>
/// </summary>
internal static class TransportDiagram
{
    private sealed record Chip(double Y, string Line1, string Line2);

    private static readonly Chip[] Inbound =
    [
        new(58, "HTTP", "POST /greet"),
        new(160, "SQS", "queue: greet"),
        new(262, "Kafka", "topic: greet"),
    ];

    private const double ChipX = 20, ChipW = 190, ChipH = 56;
    private const double HandlerX = 320, HandlerW = 210, HandlerY = 105, HandlerH = 110;
    private const double ResultX = 610, ResultW = 170, ResultH = 56;
    private const double CenterY = 160;

    public static string Render()
    {
        var chips = string.Join("\n", Inbound.Select(c => $"""
            <rect class="td-chip" x="{ChipX}" y="{c.Y - ChipH / 2:F1}" width="{ChipW}" height="{ChipH}" rx="{ChipH / 2:F1}"/>
            <text class="td-chip-label" x="{ChipX + ChipW / 2}" y="{c.Y - 4:F1}" text-anchor="middle">{Html(c.Line1)}</text>
            <text class="td-chip-sublabel" x="{ChipX + ChipW / 2}" y="{c.Y + 15:F1}" text-anchor="middle">{Html(c.Line2)}</text>
            """));

        var inboundArrows = string.Join("\n", Inbound.Select(c => $"""
            <line class="td-line" x1="{ChipX + ChipW}" y1="{c.Y}" x2="{HandlerX}" y2="{CenterY}" marker-end="url(#td-arrow)"/>
            """));

        return $"""
            <svg class="transport-diagram" viewBox="0 0 800 320" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="An HTTP POST to /greet, an SQS queue named greet, and a Kafka topic named greet all arrive at one GreetHandler for the greet topic, which returns a GreetResponse result. Adding a transport is a line of host wiring, never a change to the handler.">
              <defs>
                <marker id="td-arrow" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
                  <path class="td-arrow-head" d="M0,0 L10,5 L0,10 z"/>
                </marker>
              </defs>
              {inboundArrows}
              <line class="td-line" x1="{HandlerX + HandlerW}" y1="{CenterY}" x2="{ResultX}" y2="{CenterY}" marker-end="url(#td-arrow)"/>
              {chips}
              <rect class="td-handler" x="{HandlerX}" y="{HandlerY}" width="{HandlerW}" height="{HandlerH}" rx="14"/>
              <text class="td-handler-label" x="{HandlerX + HandlerW / 2}" y="{CenterY - 14}" text-anchor="middle">GreetHandler</text>
              <text class="td-handler-sublabel" x="{HandlerX + HandlerW / 2}" y="{CenterY + 8}" text-anchor="middle">topic: greet</text>
              <text class="td-handler-sublabel" x="{HandlerX + HandlerW / 2}" y="{CenterY + 30}" text-anchor="middle">one handler, any transport</text>
              <rect class="td-result" x="{ResultX}" y="{CenterY - ResultH / 2:F1}" width="{ResultW}" height="{ResultH}" rx="10"/>
              <text class="td-result-label" x="{ResultX + ResultW / 2}" y="{CenterY - 4}" text-anchor="middle">GreetResponse</text>
              <text class="td-result-sublabel" x="{ResultX + ResultW / 2}" y="{CenterY + 15}" text-anchor="middle">your result type</text>
            </svg>
            """;
    }

    private static string Html(string s) => System.Net.WebUtility.HtmlEncode(s);
}
