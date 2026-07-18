namespace Benzene.Mesh.Contracts;

/// <summary>A single service's exposure of a topic within a <see cref="MeshTopicEntry"/>.</summary>
public class MeshTopicService
{
    /// <summary>Initializes a new instance of the <see cref="MeshTopicService"/> class.</summary>
    /// <param name="service">The service name (matches its <see cref="MeshManifestEntry"/>).</param>
    /// <param name="httpMappings">The topic's HTTP mappings on this service, if any.</param>
    public MeshTopicService(string service, MeshTopicHttpMapping[] httpMappings)
    {
        Service = service;
        HttpMappings = httpMappings;
    }

    /// <summary>The service name (matches its <see cref="MeshManifestEntry"/>).</summary>
    public string Service { get; }

    /// <summary>The topic's HTTP mappings on this service, if any.</summary>
    public MeshTopicHttpMapping[] HttpMappings { get; }
}

/// <summary>An HTTP method+path a topic is reachable at on a service.</summary>
public class MeshTopicHttpMapping
{
    /// <summary>Initializes a new instance of the <see cref="MeshTopicHttpMapping"/> class.</summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The HTTP path.</param>
    public MeshTopicHttpMapping(string method, string path)
    {
        Method = method;
        Path = path;
    }

    /// <summary>The HTTP method.</summary>
    public string Method { get; }

    /// <summary>The HTTP path.</summary>
    public string Path { get; }
}
