using Benzene.Abstractions.Results;
using Benzene.Clients.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Results;

namespace Benzene.Examples.Mesh.OrdersService.Clients;

/// <summary>
/// A stand-in for a <c>Benzene.CodeGen.Client</c>-generated payments-api client (an
/// <see cref="IHasHealthCheck"/>), so this demo can show a consumer-side contract-drift check without
/// a live code-generation step. A real generated client's <see cref="HealthCheckAsync"/> fetches the
/// provider's <c>/healthcheck</c> over the wire and runs <see cref="ClientHealthCheckProcessor"/> to
/// compare the provider's live contract hash against the one the client was generated against; this
/// fakes the fetch with canned data (the same deterministic style as the rest of this example) and
/// runs the identical processor, so the drift verdict it produces is real.
/// </summary>
/// <remarks>
/// This check belongs on orders-api's <c>contracts</c> diagnostic topic (see <c>Startup</c>), never
/// its <c>/healthcheck</c> readiness surface: it reaches out to payments-api, so a probe that
/// included it could de-route or restart orders-api just because a downstream drifted or went slow.
/// </remarks>
public class PaymentsContractClient : IHasHealthCheck
{
    // The payments-api contract hash this "client" was generated against...
    public string HashCode => "payments-contract-v1";

    public Task<IBenzeneResult<HealthCheckResponse>> HealthCheckAsync()
    {
        // ...versus the hash payments-api currently publishes from its schema health check. They
        // differ here, so this models genuine contract drift - the same drift the demo's payments-api
        // earns at the mesh-aggregator level, seen from the consumer's side at runtime instead.
        var providerResponse = new HealthCheckResponse(true, new Dictionary<string, HealthCheckResult>
        {
            [SchemaHealthCheckConstants.Type] = (HealthCheckResult)HealthCheckResult.CreateInstance(
                true, SchemaHealthCheckConstants.Type,
                new Dictionary<string, object> { [SchemaHealthCheckConstants.HashCodeKey] = "payments-contract-v2" }),
        });

        var annotated = (HealthCheckResponse)ClientHealthCheckProcessor.Process(providerResponse, HashCode);
        return Task.FromResult(BenzeneResult.Ok(annotated));
    }
}
