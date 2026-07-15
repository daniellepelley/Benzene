namespace Benzene.Mesh.Reporting;

/// <summary>Configures where <see cref="HttpMeshReportPublisher"/> posts self-reports.</summary>
public class MeshReportingOptions
{
    /// <summary>Initializes a new instance of the <see cref="MeshReportingOptions"/> class.</summary>
    /// <param name="ingestionUrl">
    /// The aggregator's ingestion endpoint (e.g. <c>https://mesh.internal/mesh/report</c> - see
    /// <c>Benzene.Mesh.Aggregator.MeshReportMessageHandler</c>).
    /// </param>
    public MeshReportingOptions(string ingestionUrl)
    {
        IngestionUrl = ingestionUrl;
    }

    /// <summary>The aggregator's ingestion endpoint.</summary>
    public string IngestionUrl { get; }
}
