using System.Linq;
using Benzene.Abstractions.Results;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.HealthChecks;

/// <summary>
/// A consumer-side <see cref="IHealthCheck"/> that probes a single downstream provider via its
/// generated client (<see cref="IHasHealthCheck"/>) and reports two things about that provider:
/// whether it is reachable, and whether its message contract has drifted from the one this client was
/// generated against. The provider's response is already drift-annotated by the generated client's
/// <see cref="IHasHealthCheck.HealthCheckAsync"/> (which runs <see cref="ClientHealthCheckProcessor"/>);
/// this adapter folds that aggregated response down into one check result.
/// </summary>
/// <remarks>
/// Register this on the <em>contracts</em> diagnostic topic (<c>UseContractsCheck</c> +
/// <see cref="ContractHealthCheckExtensions.AddContractCheck{TClient}"/>), <strong>not</strong> a
/// liveness or readiness probe. It calls a downstream service, so a probe that included it would let
/// one struggling dependency restart or de-route otherwise-healthy pods - the anti-pattern the
/// contracts/probe separation exists to prevent (see <c>docs/kubernetes-health-checks.md</c>). Its
/// outcomes deliberately track the <em>contract</em> relationship, not the provider's transient
/// internal health (which is the provider's own readiness concern): reachable+matching contract is
/// <c>Ok</c>, reachable+drifted is <c>Warning</c> (degraded-but-not-fatal, does not flip
/// <c>IsHealthy</c>), and only an unreachable provider is <c>Failed</c>.
/// </remarks>
public class ClientHealthCheck : IHealthCheck
{
    private readonly string _serviceName;
    private readonly IHasHealthCheck _client;

    /// <summary>Initializes a new instance of the <see cref="ClientHealthCheck"/> class.</summary>
    /// <param name="serviceName">The downstream service's name, used as this check's <see cref="Type"/> and its dependency name.</param>
    /// <param name="client">The generated client for the downstream service, exposing its baked-in contract hash and a health call.</param>
    public ClientHealthCheck(string serviceName, IHasHealthCheck client)
    {
        _serviceName = serviceName;
        _client = client;
    }

    /// <inheritdoc />
    public string Type => _serviceName;

    /// <inheritdoc />
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("Service", _serviceName) };

        IBenzeneResult<HealthCheckResponse> result;
        try
        {
            result = await _client.HealthCheckAsync();
        }
        catch (Exception ex)
        {
            // IHealthCheck contract: report expected failures (e.g. connection refused) as a Failed
            // result rather than throwing. The processor's outer wrappers remain the backstop.
            return new HealthCheckResult(HealthCheckStatus.Failed, _serviceName,
                new Dictionary<string, object> { ["reachable"] = false, ["error"] = ex.Message }, dependencies);
        }

        // No health response at all -> provider unreachable. This only ever colours the contracts
        // diagnostic topic; it is never wired into a probe, so it cannot restart or de-route a pod.
        if (result?.Payload == null)
        {
            var data = new Dictionary<string, object> { ["reachable"] = false };
            if (result != null)
            {
                data["status"] = result.Status;
                if (result.Errors is { Length: > 0 })
                {
                    data["errors"] = result.Errors;
                }
            }

            return new HealthCheckResult(HealthCheckStatus.Failed, _serviceName, data, dependencies);
        }

        // Reachable: surface the contract-drift verdict already annotated onto the provider's response.
        var match = FindMatch(result.Payload);
        var data2 = new Dictionary<string, object> { ["reachable"] = true };
        if (match != null)
        {
            data2[SchemaHealthCheckConstants.MatchKey] = match;
        }

        // Genuine drift (both hashes present and differing) is degraded-but-not-fatal -> Warning, which
        // does not flip the aggregate IsHealthy. A matching contract, or no schema check to compare
        // against (can't determine drift), stays Ok - no false Warning.
        var drifted = match is { IsMatch: false, ServiceHashCode: not null };
        return drifted
            ? new HealthCheckResult(HealthCheckStatus.Warning, _serviceName, data2, dependencies)
            : new HealthCheckResult(HealthCheckStatus.Ok, _serviceName, data2, dependencies);
    }

    private static ClientHashMatch? FindMatch(HealthCheckResponse response)
    {
        var schemaCheck = response.HealthChecks
            .FirstOrDefault(x => x.Value.Type == SchemaHealthCheckConstants.Type).Value;

        return schemaCheck != null
               && schemaCheck.Data.TryGetValue(SchemaHealthCheckConstants.MatchKey, out var raw)
               && raw is ClientHashMatch match
            ? match
            : null;
    }
}
