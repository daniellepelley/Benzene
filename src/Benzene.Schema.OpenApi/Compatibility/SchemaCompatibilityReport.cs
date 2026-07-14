namespace Benzene.Schema.OpenApi.Compatibility;

/// <summary>
/// The result of comparing two versions of a service's schema: the list of classified changes and a
/// roll-up verdict. This is what a contract-testing health check turns into an ok/warning/error status.
/// </summary>
public class SchemaCompatibilityReport
{
    public SchemaCompatibilityReport(IReadOnlyList<SchemaChange> changes)
    {
        Changes = changes;
    }

    /// <summary>Every detected change, in the order they were found.</summary>
    public IReadOnlyList<SchemaChange> Changes { get; }

    /// <summary>Only the changes classified as breaking.</summary>
    public IEnumerable<SchemaChange> BreakingChanges =>
        Changes.Where(x => x.Compatibility == ChangeCompatibility.Breaking);

    /// <summary>True if any change is breaking.</summary>
    public bool HasBreakingChanges => Changes.Any(x => x.Compatibility == ChangeCompatibility.Breaking);

    /// <summary>True if any change is a warning.</summary>
    public bool HasWarnings => Changes.Any(x => x.Compatibility == ChangeCompatibility.Warning);

    /// <summary>True when there are no breaking changes (warnings are allowed).</summary>
    public bool IsCompatible => !HasBreakingChanges;

    /// <summary>The worst compatibility level across all changes (Compatible if there are none).</summary>
    public ChangeCompatibility Overall =>
        HasBreakingChanges ? ChangeCompatibility.Breaking
        : HasWarnings ? ChangeCompatibility.Warning
        : ChangeCompatibility.Compatible;
}
