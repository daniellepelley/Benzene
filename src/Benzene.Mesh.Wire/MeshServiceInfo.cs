namespace Benzene.Mesh.Wire;

/// <summary>
/// The static identity a service supplies to <see cref="MeshDescriptorFactory"/> and
/// <see cref="Extensions.UseMeshTrace{TContext}"/> (docs/specification/mesh.md §2). Every field is
/// optional; empty values simply leave the corresponding descriptor/trace fields unset.
/// </summary>
public class MeshServiceInfo
{
    public MeshServiceInfo(
        string service,
        string? serviceVersion = null,
        string? instanceId = null,
        string? binding = null,
        MeshPlacement? placement = null)
    {
        Service = service;
        ServiceVersion = serviceVersion;
        InstanceId = instanceId;
        Binding = binding;
        Placement = placement;
    }

    public string Service { get; }

    public string? ServiceVersion { get; }

    public string? InstanceId { get; }

    public string? Binding { get; }

    public MeshPlacement? Placement { get; }
}
