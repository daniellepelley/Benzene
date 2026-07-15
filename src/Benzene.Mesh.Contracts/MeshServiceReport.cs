using Benzene.HealthChecks.Core;

namespace Benzene.Mesh.Contracts;

/// <summary>
/// A service's self-reported spec/health, pushed rather than pulled - for services with no
/// synchronous entry point an <c>IMeshServiceSource</c> could poll (e.g. an SQS/SNS/EventBridge-only
/// AWS Lambda). Deliberately a narrower shape than <see cref="MeshServiceSnapshot"/>: a reporter
/// shouldn't need to compute <c>SpecHash</c>/<c>PreviousSpecHash</c>/<c>ContractDrift</c> itself -
/// whatever receives this report (see <c>IMeshReportPublisher</c>) builds the full snapshot,
/// the same way a pulled fetch does, so both compute drift identically.
/// </summary>
public class MeshServiceReport
{
    /// <summary>Initializes a new instance of the <see cref="MeshServiceReport"/> class.</summary>
    /// <param name="name">The service's name (matches its registry entry's <c>Name</c>).</param>
    /// <param name="reportedAtUtc">When this report was generated.</param>
    /// <param name="specJson">The service's current spec document, verbatim. <c>null</c> if unavailable.</param>
    /// <param name="health">The service's current aggregated health check response. <c>null</c> if unavailable.</param>
    /// <param name="error">The type name of an exception encountered while building the report, if any - never the message.</param>
    public MeshServiceReport(string name, DateTimeOffset reportedAtUtc, string? specJson, HealthCheckResponse? health, string? error)
    {
        Name = name;
        ReportedAtUtc = reportedAtUtc;
        SpecJson = specJson;
        Health = health;
        Error = error;
    }

    /// <summary>The service's name (matches its registry entry's <c>Name</c>).</summary>
    public string Name { get; }

    /// <summary>When this report was generated.</summary>
    public DateTimeOffset ReportedAtUtc { get; }

    /// <summary>The service's current spec document, verbatim. <c>null</c> if unavailable.</summary>
    public string? SpecJson { get; }

    /// <summary>The service's current aggregated health check response. <c>null</c> if unavailable.</summary>
    public HealthCheckResponse? Health { get; }

    /// <summary>The type name of an exception encountered while building the report, if any - never the message.</summary>
    public string? Error { get; }
}
