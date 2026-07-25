namespace Benzene.Abstractions;

/// <summary>
/// The framework-owned reserved topic ids — Benzene's own endpoints, as opposed to the
/// application's topics (see <c>docs/specification/cloud-service-profile.md</c> and
/// <c>docs/specification/mesh.md</c> §1).
/// <para>
/// Every id here carries the <see cref="Prefix"/> marker, per the naming principle
/// (<c>work/benzene-naming-principle.md</c>): <b>where Benzene puts a name into a namespace it
/// shares with someone else, that name is marked as Benzene's.</b> Topic ids share a namespace
/// with the application's own topics — <c>order:create</c> sits beside these — so a bare
/// <c>report</c> or <c>ping</c> would be a collision waiting to happen. The prefix also makes
/// "is this Benzene's?" answerable by inspection (<see cref="IsReserved"/>) instead of by
/// consulting a list that goes stale.
/// </para>
/// <para>
/// This is the single source of truth for these ids, and the topic-side counterpart of
/// <c>BenzeneResultStatus</c> (the status vocabulary). Reference these constants rather than
/// re-typing the literal: a duplicated string is how the two drift apart.
/// </para>
/// <para>
/// The mesh's own wire topics (<c>benzene:mesh:register</c>, <c>benzene:mesh:query:fleet</c>, …)
/// live in <c>MeshTopics</c> in <c>Benzene.Mesh.Wire</c>, next to the contract they serve — the
/// mesh is an optional add-on and this root abstraction deliberately doesn't know about it. They
/// carry the same prefix, so <see cref="IsReserved"/> recognises them too.
/// </para>
/// </summary>
public static class BenzeneTopic
{
    /// <summary>
    /// The marker every framework-owned topic id starts with. Note the trailing <c>:</c> — the
    /// existing namespace separator for topic ids (<c>benzene:mesh:query:fleet</c>).
    /// </summary>
    public const string Prefix = "benzene:";

    /// <summary>The service's own contract document (Cloud Service Profile R5).</summary>
    public const string Spec = "benzene:spec";

    /// <summary>Example payloads a caller can use to exercise the service's topics.</summary>
    public const string TestPayloads = "benzene:test-payloads";

    /// <summary>The deep health check — dependencies included (Profile R3).</summary>
    public const string HealthCheck = "benzene:healthcheck";

    /// <summary>Liveness: is the process up? Never gated on dependencies.</summary>
    public const string Liveness = "benzene:liveness";

    /// <summary>Readiness: should this instance receive traffic?</summary>
    public const string Readiness = "benzene:readiness";

    /// <summary>The mesh descriptor topic a meshed service intercepts (mesh spec §1, Profile R6).</summary>
    public const string Mesh = "benzene:mesh";

    /// <summary>The transport reachability probe used by the queue/stream health checks.</summary>
    public const string Ping = "benzene:ping";

    // Deliberately absent, and both were surfaced by the 2026-07-25 rename:
    //   "invoke" — the old reserved list carried it, but it is an HTTP PATH (/benzene/invoke), never
    //     a topic id. Declaring "benzene:invoke" would invent wire surface nothing routes.
    //   "report" — the real id is MeshTopics.Report ("benzene:mesh:report"); the bare entry was an
    //     alias in the filter list that no handler ever bound to.

    private static readonly HashSet<string> ReservedIds = new(StringComparer.OrdinalIgnoreCase)
    {
        Spec,
        TestPayloads,
        HealthCheck,
        Liveness,
        Readiness,
        Mesh,
        Ping
    };

    /// <summary>
    /// Whether <paramref name="topic"/> is a framework-owned topic — any id carrying the
    /// <see cref="Prefix"/>, which covers the ids above <i>and</i> the mesh wire topics and any
    /// future framework topic. Tooling that hides Benzene's own traffic (the mesh UI's utility
    /// filter) should use this rather than maintaining a name list.
    /// </summary>
    public static bool IsReserved(string? topic)
        => !string.IsNullOrEmpty(topic) && topic!.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether <paramref name="topic"/> is one of the specific ids declared on this type — a
    /// narrower test than <see cref="IsReserved"/>, which accepts any prefixed id.
    /// </summary>
    public static bool IsKnown(string? topic) => topic is not null && ReservedIds.Contains(topic);

    /// <summary>The ids declared on this type, for tooling that needs to enumerate them.</summary>
    public static IReadOnlyCollection<string> All => ReservedIds;
}
