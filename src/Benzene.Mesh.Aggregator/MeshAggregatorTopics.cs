namespace Benzene.Mesh.Aggregator;

/// <summary>
/// The aggregator host's own operational topics — the ones it exposes to trigger or feed an
/// aggregation run, as opposed to the cross-service wire contract (<c>MeshTopics</c> in
/// <c>Benzene.Mesh.Wire</c>, which this package deliberately does not reference).
/// <para>
/// All ids carry the <c>benzene:</c> marker per the naming principle
/// (<c>work/benzene-naming-principle.md</c>): a topic id sits in the same namespace as the
/// application's own topics, so it says whose it is. Reference these constants rather than
/// re-typing the literal.
/// </para>
/// </summary>
public static class MeshAggregatorTopics
{
    /// <summary>Triggers an aggregation pass (discover, fetch, compose, publish).</summary>
    public const string Aggregate = "benzene:mesh:aggregate";

    /// <summary>Ingests a service's self-report, for the push half of discovery.</summary>
    public const string Report = "benzene:mesh:report";

    /// <summary>Appends a discussion annotation to a topic or service.</summary>
    public const string AnnotationsAdd = "benzene:mesh:annotations:add";
}
