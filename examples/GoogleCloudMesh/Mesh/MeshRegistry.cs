using Benzene.Mesh.Contracts;

namespace Benzene.Examples.GoogleCloudMesh.Mesh;

/// <summary>
/// Builds the static mesh service registry from per-service base-URL environment variables
/// (<c>MESH_ORDERS_URL</c>, <c>MESH_PAYMENTS_URL</c>, …), each pointing at a service's HTTP function.
/// Google Cloud has no mesh discovery provider yet, so the registry is supplied explicitly (from the
/// Terraform-emitted function URLs) rather than discovered by tag — a clean follow-on is a
/// <c>Benzene.Mesh.Discovery.Google</c> that lists Cloud Functions by label.
/// </summary>
public static class MeshRegistry
{
    private static readonly string[] Services = { "orders", "payments", "shipping", "notifications" };

    public static MeshServiceRegistry FromEnvironment()
    {
        var entries = new List<MeshServiceRegistryEntry>();
        foreach (var name in Services)
        {
            var baseUrl = Environment.GetEnvironmentVariable($"MESH_{name.ToUpperInvariant()}_URL");
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                continue;
            }
            var root = baseUrl.TrimEnd('/');
            entries.Add(new MeshServiceRegistryEntry(
                name,
                specUrl: $"{root}/benzene/spec?type=benzene",
                healthUrl: $"{root}/benzene/health"));
        }
        return new MeshServiceRegistry(entries.ToArray());
    }
}
