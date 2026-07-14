namespace Benzene.Mesh.Contracts;

/// <summary>
/// The set of services a <c>Benzene.Mesh.Aggregator</c> polls on each run - the mesh.json registry
/// config. Human-maintained, not generated.
/// </summary>
public class MeshServiceRegistry
{
    /// <summary>Initializes a new instance of the <see cref="MeshServiceRegistry"/> class.</summary>
    /// <param name="services">The services to poll.</param>
    public MeshServiceRegistry(MeshServiceRegistryEntry[] services)
    {
        Services = services;
    }

    /// <summary>The services to poll.</summary>
    public MeshServiceRegistryEntry[] Services { get; }
}
