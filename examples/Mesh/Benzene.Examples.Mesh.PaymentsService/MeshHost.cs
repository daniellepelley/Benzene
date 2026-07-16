using Benzene.Examples.Mesh.PaymentsService.Handlers;
using Benzene.Examples.Mesh.Shared;
using Benzene.Mesh.Wire;

namespace Benzene.Examples.Mesh.PaymentsService;

/// <summary>
/// This service's meshed wire-envelope host. The DEMO_ADD_ENDPOINT topic changes the derived
/// descriptor (and so its hash) exactly as it changes the OpenAPI spec - the same contract-drift
/// story the aggregator demo tells, visible on the Fleet view as a hash mismatch until the
/// restarted instance re-registers. DEMO_PAYMENTS_HEALTHY drives the heartbeat, so the Fleet view
/// mirrors the dashboard's unhealthy badge.
/// </summary>
public static class MeshHost
{
    public static readonly EnvelopeHost Instance = new(
        Environment.GetEnvironmentVariable("DEMO_ADD_ENDPOINT") == "true"
            ? new[] { typeof(GetPaymentMessageHandler), typeof(GetPaymentRefundsMessageHandler) }
            : new[] { typeof(GetPaymentMessageHandler) },
        meshInfo: new MeshServiceInfo("payments-api", "1.0.0", "payments-api-1", "http",
            new MeshPlacement { Cloud = "self-hosted" }),
        collectorEnvelopeUrl: Environment.GetEnvironmentVariable("MESH_COLLECTOR_ENVELOPE_URL")
                              ?? "http://localhost:5300/benzene/invoke",
        healthy: () => Environment.GetEnvironmentVariable("DEMO_PAYMENTS_HEALTHY") == "true");
}
