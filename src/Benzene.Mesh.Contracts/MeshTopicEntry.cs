namespace Benzene.Mesh.Contracts;

/// <summary>One distinct topic in a <see cref="MeshTopicCatalog"/> and the services that expose it.</summary>
public class MeshTopicEntry
{
    /// <summary>Initializes a new instance of the <see cref="MeshTopicEntry"/> class.</summary>
    /// <param name="topic">The topic id.</param>
    /// <param name="reserved">True when this is a reserved Benzene utility topic (spec/health/mesh/…) rather than a domain topic.</param>
    /// <param name="services">The services exposing this topic.</param>
    public MeshTopicEntry(string topic, bool reserved, MeshTopicService[] services)
    {
        Topic = topic;
        Reserved = reserved;
        Services = services;
    }

    /// <summary>The topic id.</summary>
    public string Topic { get; }

    /// <summary>True when this is a reserved Benzene utility topic rather than a domain topic.</summary>
    public bool Reserved { get; }

    /// <summary>The services exposing this topic.</summary>
    public MeshTopicService[] Services { get; }
}
