namespace Benzene.Mesh.Contracts;

/// <summary>
/// One entry in a <see cref="MeshServiceRegistry"/> - a human-maintained record of where to find a
/// service's spec and health endpoints. Not generated; this is the input a
/// <c>Benzene.Mesh.Aggregator</c> polls.
/// </summary>
public class MeshServiceRegistryEntry
{
    /// <summary>Initializes a new instance of the <see cref="MeshServiceRegistryEntry"/> class.</summary>
    /// <param name="name">The service's name, used as its key across all generated mesh artifacts.</param>
    /// <param name="specUrl">The URL to fetch the service's spec document from (e.g. <c>https://.../spec?type=benzene</c>).</param>
    /// <param name="healthUrl">The URL to fetch the service's aggregated health check response from.</param>
    public MeshServiceRegistryEntry(string name, string specUrl, string healthUrl)
    {
        Name = name;
        SpecUrl = specUrl;
        HealthUrl = healthUrl;
    }

    /// <summary>The service's name, used as its key across all generated mesh artifacts.</summary>
    public string Name { get; }

    /// <summary>The URL to fetch the service's spec document from (e.g. <c>https://.../spec?type=benzene</c>).</summary>
    public string SpecUrl { get; }

    /// <summary>The URL to fetch the service's aggregated health check response from.</summary>
    public string HealthUrl { get; }
}
