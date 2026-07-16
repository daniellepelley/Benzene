using Benzene.Examples.Mesh.Shared;
using Benzene.Examples.Mesh.ShippingService.Handlers;
using Benzene.Mesh.Wire;

namespace Benzene.Examples.Mesh.ShippingService;

/// <summary>This service's meshed wire-envelope host - not started by default (run.sh leaves
/// shipping-api down), so the Fleet view demonstrates its absence honestly: no row until it
/// starts and registers.</summary>
public static class MeshHost
{
    public static readonly EnvelopeHost Instance = new(
        new[] { typeof(GetShipmentMessageHandler) },
        meshInfo: new MeshServiceInfo("shipping-api", "1.0.0", "shipping-api-1", "http",
            new MeshPlacement { Cloud = "self-hosted" }),
        collectorEnvelopeUrl: Environment.GetEnvironmentVariable("MESH_COLLECTOR_ENVELOPE_URL")
                              ?? "http://localhost:5300/benzene/invoke");
}
