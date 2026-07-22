namespace Benzene.Mesh.Contracts;

/// <summary>
/// A topic-level (across all of its versions) reconciliation of which payload <em>versions</em> the fleet
/// <b>produces</b> against which it <b>consumes</b> — the "are producers and consumers version-compatible"
/// view. Aggregator-computed from every service's self-description (spec <c>events</c>/<c>requests</c>),
/// one per non-reserved topic id that has more than one version in play or an outright version skew.
/// <para>
/// A version a producer emits but no consumer handles at that exact version
/// (<see cref="ProducedNotConsumed"/>) is the load-bearing signal — a forward-compatibility risk — with one
/// honest caveat the mesh cannot see: an upcaster (<c>Benzene.Core.Versioning</c>) registered on the
/// consumer may transparently bridge that older version to the handler's schema, so a skew here is a
/// prompt to confirm the upcaster exists, not a proven break.
/// </para>
/// </summary>
public class MeshTopicVersionCompatibility
{
    /// <summary>Initializes a new instance of the <see cref="MeshTopicVersionCompatibility"/> class.</summary>
    /// <param name="topic">The topic id (versions are the axis this reconciles).</param>
    /// <param name="producedVersions">Every version some service declares producing (empty string = the unversioned producer).</param>
    /// <param name="consumedVersions">Every version some service handles (empty string = the unversioned handler).</param>
    /// <param name="producedNotConsumed">Versions produced somewhere but handled by no service at that version — see the type remarks.</param>
    /// <param name="consumedNotProduced">Versions handled somewhere but produced by no service — a stale handler, or a version being retired.</param>
    public MeshTopicVersionCompatibility(string topic, string[] producedVersions, string[] consumedVersions,
        string[] producedNotConsumed, string[] consumedNotProduced)
    {
        Topic = topic;
        ProducedVersions = producedVersions;
        ConsumedVersions = consumedVersions;
        ProducedNotConsumed = producedNotConsumed;
        ConsumedNotProduced = consumedNotProduced;
    }

    /// <summary>The topic id.</summary>
    public string Topic { get; }

    /// <summary>Every version some service declares producing (empty string = unversioned).</summary>
    public string[] ProducedVersions { get; }

    /// <summary>Every version some service handles (empty string = unversioned).</summary>
    public string[] ConsumedVersions { get; }

    /// <summary>
    /// Versions produced somewhere in the fleet that <b>no</b> service handles at that exact version. The
    /// key compatibility signal (see the type remarks) — non-empty means a producer is emitting a version
    /// nothing structurally handles; confirm an upcaster covers it.
    /// </summary>
    public string[] ProducedNotConsumed { get; }

    /// <summary>Versions handled somewhere that <b>no</b> service produces — a stale handler or a retiring version.</summary>
    public string[] ConsumedNotProduced { get; }

    /// <summary>True when every produced version has a matching consumer at that version (no <see cref="ProducedNotConsumed"/>).</summary>
    public bool IsCompatible => ProducedNotConsumed.Length == 0;
}
