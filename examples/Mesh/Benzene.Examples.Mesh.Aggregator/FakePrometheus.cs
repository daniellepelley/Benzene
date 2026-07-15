using Microsoft.AspNetCore.Http;

/// <summary>
/// A tiny in-process stand-in for a real Prometheus-compatible endpoint, so this example can
/// demonstrate <c>Benzene.Mesh.Tracing.Tempo</c> end to end without a real Tempo/Prometheus stack
/// (Docker, network egress) - matches how the rest of examples/Mesh already fakes health/spec data
/// deterministically. Answers the exact 5 PromQL shapes
/// <c>TempoServiceGraphTopologyBuilder.BuildAsync</c> issues with canned samples for 3 illustrative
/// edges. See examples/Mesh/README.md for what each edge is meant to demonstrate.
/// </summary>
public static class FakePrometheus
{
    private static readonly (string Client, string Server, double RequestsPerMinute, double? FailedPerMinute, double P50, double P95, double P99)[] Edges =
    {
        // Echoes payments-api's "unhealthy" badge elsewhere on the dashboard - the same story,
        // confirmed independently by observed traffic rather than just a health check.
        ("orders-api", "payments-api", 86.4, 15.55, 45, 420, 890),
        // Healthy-looking traffic to a service that's unreachable *right now* (shipping-api isn't
        // started by default) - topology data is an independent signal from live health; recent
        // traffic can exist for a service that's down this instant.
        ("orders-api", "shipping-api", 24.1, 0.096, 12, 35, 58),
        // Low, clean traffic. FailedPerMinute is deliberately null (not 0) - real Prometheus never
        // emits a rate() sample for a metric that's never incremented.
        ("payments-api", "shipping-api", 6.2, null, 8, 15, 22),
    };

    public static IResult Handle(HttpContext context)
    {
        var query = context.Request.Query["query"].ToString();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        object[] result;
        if (query.Contains("_failed_total"))
        {
            result = Samples(timestamp, e => e.FailedPerMinute);
        }
        else if (query.Contains("_total"))
        {
            result = Samples(timestamp, e => e.RequestsPerMinute);
        }
        else if (query.Contains("histogram_quantile(0.50"))
        {
            result = Samples(timestamp, e => e.P50);
        }
        else if (query.Contains("histogram_quantile(0.95"))
        {
            result = Samples(timestamp, e => e.P95);
        }
        else if (query.Contains("histogram_quantile(0.99"))
        {
            result = Samples(timestamp, e => e.P99);
        }
        else
        {
            result = Array.Empty<object>();
        }

        return Results.Json(new { status = "success", data = new { resultType = "vector", result } });
    }

    private static object[] Samples(long timestamp, Func<(string Client, string Server, double RequestsPerMinute, double? FailedPerMinute, double P50, double P95, double P99), double?> select)
    {
        var samples = new List<object>();
        foreach (var edge in Edges)
        {
            var value = select(edge);
            if (value == null)
            {
                continue;
            }

            samples.Add(new
            {
                metric = new { client = edge.Client, server = edge.Server },
                value = new object[] { timestamp, value.Value.ToString("0.###") },
            });
        }

        return samples.ToArray();
    }
}
