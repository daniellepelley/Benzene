using System.Text;

namespace Benzene.Schema.OpenApi.Compatibility;

/// <summary>
/// Thrown by <see cref="SchemaCompatibility.EnsureBackwardCompatible(EventService.EventServiceDocument, EventService.EventServiceDocument, SchemaCompatibilityRules?)"/>
/// when the current message contract has one or more breaking changes versus the baseline. The
/// message lists each breaking change; <see cref="Report"/> carries the full comparison (including
/// non-breaking changes and warnings).
/// </summary>
public class SchemaCompatibilityException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaCompatibilityException"/> class.
    /// </summary>
    /// <param name="report">The compatibility report containing the breaking changes.</param>
    public SchemaCompatibilityException(SchemaCompatibilityReport report)
        : base(BuildMessage(report))
    {
        Report = report;
    }

    /// <summary>Gets the full compatibility report.</summary>
    public SchemaCompatibilityReport Report { get; }

    private static string BuildMessage(SchemaCompatibilityReport report)
    {
        var breaking = report.BreakingChanges.ToList();

        var builder = new StringBuilder();
        builder.AppendLine(
            $"The current message contract is not backward compatible with the baseline - {breaking.Count} breaking change(s):");
        foreach (var change in breaking)
        {
            builder.AppendLine($"  - {change}");
        }

        return builder.ToString().TrimEnd();
    }
}
