namespace Benzene.Mesh.Contracts;

/// <summary>
/// One detected difference for a (topic, version) between the previous aggregator run's
/// <c>topics.json</c> and this one - the topic-level "what changed" substance a plain
/// contract-drift hash can't give. Computed by <c>Benzene.Mesh.Aggregator</c> from its own
/// previous artifact; never a claim any service makes about itself.
/// </summary>
public class MeshTopicChange
{
    /// <summary>Initializes a new instance of the <see cref="MeshTopicChange"/> class.</summary>
    /// <param name="kind">One of <see cref="MeshTopicChangeKind"/>.</param>
    /// <param name="description">A human-readable description of the change.</param>
    public MeshTopicChange(string kind, string description)
    {
        Kind = kind;
        Description = description;
    }

    /// <summary>One of <see cref="MeshTopicChangeKind"/>.</summary>
    public string Kind { get; }

    /// <summary>A human-readable description of the change.</summary>
    public string Description { get; }
}
