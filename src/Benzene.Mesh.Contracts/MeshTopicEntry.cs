namespace Benzene.Mesh.Contracts;

/// <summary>
/// One distinct (topic, version) pair in a <see cref="MeshTopicCatalog"/>, and every service in
/// the fleet that produces or consumes it. Aggregator-computed output, derived entirely from each
/// service's own self-description (spec <c>requests</c>/<c>events</c>) — never a claim any single
/// service makes about itself. See <see cref="Status"/>.
/// </summary>
public class MeshTopicEntry
{
    /// <summary>Initializes a new instance of the <see cref="MeshTopicEntry"/> class.</summary>
    /// <param name="topic">The topic id.</param>
    /// <param name="version">The topic's handler version (empty for the unversioned handler).</param>
    /// <param name="reserved">True when this is a reserved Benzene utility topic (spec/health/mesh/…) rather than a domain topic.</param>
    /// <param name="consumers">The services that handle this topic (spec <c>requests</c>).</param>
    /// <param name="producers">The services that declare sending this topic (spec <c>events</c>).</param>
    /// <param name="status">One of <see cref="MeshTopicStatus"/>, or <c>null</c> when neither signal applies — see <see cref="Status"/>.</param>
    public MeshTopicEntry(string topic, string version, bool reserved,
        MeshTopicService[] consumers, MeshTopicProducer[] producers, string? status)
    {
        Topic = topic;
        Version = version;
        Reserved = reserved;
        Consumers = consumers;
        Producers = producers;
        Status = status;
    }

    /// <summary>The topic id.</summary>
    public string Topic { get; }

    /// <summary>The topic's handler version (empty for the unversioned handler).</summary>
    public string Version { get; }

    /// <summary>True when this is a reserved Benzene utility topic rather than a domain topic.</summary>
    public bool Reserved { get; }

    /// <summary>The services that handle this topic (spec <c>requests</c>).</summary>
    public MeshTopicService[] Consumers { get; }

    /// <summary>The services that declare sending this topic (spec <c>events</c>).</summary>
    public MeshTopicProducer[] Producers { get; }

    /// <summary>
    /// An informational signal computed from <see cref="Producers"/>/<see cref="Consumers"/> —
    /// never present on a reserved topic (a health check has no "producer" in this sense).
    /// One of <see cref="MeshTopicStatus"/>, or <c>null</c> when the topic looks ordinary (has
    /// both producers and consumers, or is a plain HTTP-invoked endpoint with no fleet-internal
    /// producer expected in the first place). Neither non-null value is an error: a
    /// <see cref="MeshTopicStatus.DeprecationCandidate"/> topic may simply not have been retired
    /// yet, and a <see cref="MeshTopicStatus.Gap"/> topic may legitimately be fed by a third party
    /// or a non-Benzene system outside this fleet.
    /// </summary>
    public string? Status { get; }
}
