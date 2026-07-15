using System.Text;
using System.Text.Json;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Reporting;

/// <summary>
/// The HTTP-ingestion <see cref="IMeshReportPublisher"/> - POSTs the report as JSON to
/// <see cref="MeshReportingOptions.IngestionUrl"/> (an aggregator's
/// <c>Benzene.Mesh.Aggregator.MeshReportMessageHandler</c> endpoint). For a reporter that isn't
/// colocated with the aggregator's own storage - see
/// <c>Benzene.Mesh.Aggregator.ArtifactStoreMeshReportPublisher</c> for the colocated alternative.
/// </summary>
public class HttpMeshReportPublisher : IMeshReportPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _httpClient;
    private readonly MeshReportingOptions _options;

    /// <summary>Initializes a new instance of the <see cref="HttpMeshReportPublisher"/> class.</summary>
    /// <param name="httpClient">The client used to post reports.</param>
    /// <param name="options">Where to post reports.</param>
    public HttpMeshReportPublisher(HttpClient httpClient, MeshReportingOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public async Task PublishAsync(MeshServiceReport report)
    {
        var json = JsonSerializer.Serialize(report, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(_options.IngestionUrl, content);
        response.EnsureSuccessStatusCode();
    }
}
