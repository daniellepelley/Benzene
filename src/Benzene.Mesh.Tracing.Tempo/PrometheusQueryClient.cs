using System.Globalization;
using System.Text.Json;

namespace Benzene.Mesh.Tracing.Tempo;

/// <summary>
/// A minimal client for Prometheus's HTTP instant-query API
/// (<c>GET /api/v1/query?query=...</c>) - the query surface Tempo's metrics-generator
/// remote-writes service-graph metrics to (see <c>work/service-mesh-roadmap-1.0.md</c> §4.6.1).
/// </summary>
public class PrometheusQueryClient
{
    private readonly HttpClient _httpClient;

    /// <summary>Initializes a new instance of the <see cref="PrometheusQueryClient"/> class.</summary>
    /// <param name="httpClient">The client used to call the Prometheus-compatible endpoint.</param>
    public PrometheusQueryClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Runs an instant PromQL query and returns its result as a flat list of samples. Never
    /// throws for a reachable-but-unsuccessful response (an HTTP error, a Prometheus
    /// <c>"status":"error"</c> body, or a malformed/unexpected body all surface as an empty
    /// result) - matches <c>Benzene.Mesh.Aggregator.MeshAggregator</c>'s existing philosophy that
    /// one bad query shouldn't prevent the rest of a topology build from completing. Genuine
    /// connection-level failures (DNS, refused connection, timeout) still throw.
    /// </summary>
    /// <param name="prometheusUrl">The Prometheus-compatible instant-query endpoint.</param>
    /// <param name="promQl">The PromQL expression to evaluate.</param>
    /// <returns>The matched timeseries samples, or an empty list if the query failed or returned nothing.</returns>
    public async Task<IReadOnlyList<PrometheusSample>> QueryAsync(string prometheusUrl, string promQl)
    {
        var url = $"{prometheusUrl}?query={Uri.EscapeDataString(promQl)}";
        var response = await _httpClient.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();

        try
        {
            return ParseVectorResult(body);
        }
        catch (JsonException)
        {
            return Array.Empty<PrometheusSample>();
        }
    }

    private static IReadOnlyList<PrometheusSample> ParseVectorResult(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (!root.TryGetProperty("status", out var status) || status.GetString() != "success")
        {
            return Array.Empty<PrometheusSample>();
        }

        if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("result", out var result))
        {
            return Array.Empty<PrometheusSample>();
        }

        var samples = new List<PrometheusSample>();
        foreach (var entry in result.EnumerateArray())
        {
            if (!entry.TryGetProperty("metric", out var metricElement) ||
                !entry.TryGetProperty("value", out var valueElement) ||
                valueElement.GetArrayLength() != 2)
            {
                continue;
            }

            var labels = new Dictionary<string, string>();
            foreach (var label in metricElement.EnumerateObject())
            {
                labels[label.Name] = label.Value.GetString() ?? string.Empty;
            }

            var rawValue = valueElement[1].GetString();
            if (rawValue == null || !double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                continue;
            }

            samples.Add(new PrometheusSample(labels, value));
        }

        return samples;
    }
}
