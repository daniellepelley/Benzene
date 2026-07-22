namespace Benzene.Mesh.Contracts;

/// <summary>
/// A (topic, version) that was declared somewhere in the previous aggregator run's catalog but is
/// declared nowhere in this one - it can't be flagged on a current <see cref="MeshTopicEntry"/>
/// because there no longer is one. For the estate's value/deprecation review this is first-class
/// evidence: either a retirement that just completed, or a disappearance someone should confirm
/// was intended.
/// </summary>
public class MeshRemovedTopic
{
    /// <summary>Initializes a new instance of the <see cref="MeshRemovedTopic"/> class.</summary>
    /// <param name="topic">The topic id.</param>
    /// <param name="version">The topic's handler version (empty for the unversioned handler).</param>
    public MeshRemovedTopic(string topic, string version)
    {
        Topic = topic;
        Version = version;
    }

    /// <summary>The topic id.</summary>
    public string Topic { get; }

    /// <summary>The topic's handler version (empty for the unversioned handler).</summary>
    public string Version { get; }
}
