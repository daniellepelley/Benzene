namespace Benzene.Website.Generator;

/// <summary>
/// A static SVG diagram of Benzene's hexagonal architecture: one central hexagon (your message
/// handlers) with a connecting line to one outer hexagon per adapter - literally six sides for
/// six adapters, tying the "hexagonal architecture" name back to the shape itself.
/// </summary>
internal static class ArchitectureDiagram
{
    private sealed record Node(double X, double Y, string Label, string Anchor, double LabelX, double LabelY);

    private static readonly Node[] Nodes =
    [
        new(300, 60, "ASP.NET Core", "middle", 300, 108),
        new(429.9, 135, "AWS Lambda", "start", 466, 140),
        new(429.9, 285, "Azure Functions", "start", 466, 290),
        new(300, 360, "Kafka", "middle", 300, 400),
        new(170.1, 285, "Cloudflare", "end", 134, 290),
        new(170.1, 135, "gRPC / Worker", "end", 134, 140),
    ];

    private const double CenterX = 300;
    private const double CenterY = 210;

    public static string Render()
    {
        var lines = string.Join("\n", Nodes.Select(n =>
            $"""<line x1="{CenterX}" y1="{CenterY}" x2="{n.X:F1}" y2="{n.Y:F1}"/>"""));

        var nodeShapes = string.Join("\n", Nodes.Select(n =>
            $"""
            <polygon class="node" points="{HexagonPoints(n.X, n.Y, 26)}"/>
            <text class="node-label" x="{n.LabelX}" y="{n.LabelY}" text-anchor="{n.Anchor}">{System.Net.WebUtility.HtmlEncode(n.Label)}</text>
            """));

        return $"""
            <svg class="arch-diagram" viewBox="0 0 600 440" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="Benzene's hexagonal architecture: your message handlers in the center, with an adapter for ASP.NET Core, AWS Lambda, Azure Functions, Kafka, Cloudflare, and gRPC/Worker hosts on each side">
              {lines}
              <polygon class="core" points="{HexagonPoints(CenterX, CenterY, 68)}"/>
              <text class="core-label" x="{CenterX}" y="{CenterY - 6}" text-anchor="middle">Your Message</text>
              <text class="core-label" x="{CenterX}" y="{CenterY + 14}" text-anchor="middle">Handlers</text>
              {nodeShapes}
            </svg>
            """;
    }

    private static string HexagonPoints(double cx, double cy, double r)
    {
        double[] angles = [-90, -30, 30, 90, 150, 210];
        return string.Join(" ", angles.Select(a =>
        {
            var rad = a * Math.PI / 180;
            return $"{cx + r * Math.Cos(rad):F1},{cy + r * Math.Sin(rad):F1}";
        }));
    }
}
