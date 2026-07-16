using Benzene.Examples.Mesh.OrdersService.Handlers;
using Benzene.Examples.Mesh.Shared;
using Benzene.Mesh.Wire;

namespace Benzene.Examples.Mesh.OrdersService;

/// <summary>This service's meshed wire-envelope host: descriptor + trace feed pointed at the
/// collector, serving orders-api's topics at POST /invoke.</summary>
public static class MeshHost
{
    public static readonly EnvelopeHost Instance = new(
        new[] { typeof(GetOrdersMessageHandler), typeof(CheckoutOrderMessageHandler) },
        meshInfo: new MeshServiceInfo("orders-api", "1.0.0", "orders-api-1", "http",
            new MeshPlacement { Cloud = "self-hosted" }),
        collectorEnvelopeUrl: Environment.GetEnvironmentVariable("MESH_COLLECTOR_ENVELOPE_URL")
                              ?? "http://localhost:5300/invoke");
}
