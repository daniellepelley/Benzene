using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Benzene.Diagnostics;

/// <summary>
/// Provides the shared <see cref="System.Diagnostics.ActivitySource"/> and <see cref="System.Diagnostics.Metrics.Meter"/>
/// every Benzene pipeline stage reports through. Listen with an <see cref="ActivityListener"/> for
/// spans, or wire <c>Benzene.OpenTelemetry</c>'s instrumentation extensions to export both to a real backend.
/// </summary>
public static class BenzeneDiagnostics
{
    /// <summary>The name shared by <see cref="ActivitySource"/> and <see cref="Meter"/>.</summary>
    public const string SourceName = "Benzene";

    /// <summary>The <see cref="System.Diagnostics.ActivitySource"/> every pipeline stage starts an <see cref="Activity"/> on.</summary>
    public static readonly ActivitySource ActivitySource = new(SourceName);

    /// <summary>The <see cref="System.Diagnostics.Metrics.Meter"/> Benzene's built-in instruments are recorded on.</summary>
    public static readonly Meter Meter = new(SourceName);
}
