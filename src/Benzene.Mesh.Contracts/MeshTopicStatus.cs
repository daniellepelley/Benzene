namespace Benzene.Mesh.Contracts;

/// <summary>
/// The informational signal values <see cref="MeshTopicEntry.Status"/> can report. Loose string
/// constants rather than an enum, matching <see cref="MeshServiceStatus"/>'s existing convention.
/// Neither value is an error — both are prompts to look, not failures.
/// </summary>
public static class MeshTopicStatus
{
    /// <summary>
    /// This (topic, version) has at least one producer declared somewhere in the fleet, but no
    /// service handles it anymore. Nothing is consuming it — a candidate for retiring, not proof
    /// that it's already safe to delete (a consumer outside this fleet's registry, or simply not
    /// currently traced, can't be ruled out from structural data alone).
    /// </summary>
    public const string DeprecationCandidate = "deprecation-candidate";

    /// <summary>
    /// This (topic, version) is consumed by at least one service, entirely through non-HTTP
    /// bindings, but no service in the fleet declares producing it. Not necessarily a problem —
    /// the producer may be a third party or a system outside this Benzene fleet entirely (e.g.
    /// something writing straight to a queue) — but worth surfacing so someone can confirm that's
    /// expected rather than a wiring gap.
    /// </summary>
    public const string Gap = "gap";
}
