using System.Linq;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.HealthChecks;

/// <summary>
/// The consumer side of the contract-drift check: compares the contract hash a client was generated
/// against with the provider's live contract hash, published by the provider's
/// <c>Benzene.HealthChecks.Schema.SchemaHealthCheck</c> in its <see cref="SchemaHealthCheckConstants.Type"/>
/// health check.
/// </summary>
public class ClientHealthCheckProcessor
{
    /// <summary>
    /// Reads the provider's schema hash out of <paramref name="healthCheckResponse"/>, compares it
    /// with <paramref name="hashCode"/> (the hash the client was generated against), and writes the
    /// verdict as a <see cref="ClientHashMatch"/> into the schema health check's data.
    /// </summary>
    /// <param name="healthCheckResponse">The provider's health-check response, as returned from its healthcheck endpoint.</param>
    /// <param name="hashCode">The contract hash the consumer's generated client was built against.</param>
    /// <returns>The response, with the schema health check annotated with the hash-match verdict.</returns>
    public static IHealthCheckResponse<HealthCheckResult> Process(IHealthCheckResponse<HealthCheckResult> healthCheckResponse, string hashCode)
    {
        var entry = healthCheckResponse.HealthChecks
            .FirstOrDefault(x => x.Value.Type == SchemaHealthCheckConstants.Type);

        // No schema health check to compare against - nothing to annotate, pass the response through.
        if (entry.Value == null)
        {
            return new HealthCheckResponse(healthCheckResponse.IsHealthy, healthCheckResponse.HealthChecks);
        }

        var schemaHealthCheck = entry.Value;

        // The hash is published as a plain string, but after the response is serialized over the wire
        // and deserialized it may arrive boxed as a JsonElement (System.Text.Json) or a JToken
        // (Newtonsoft) rather than a string - ToString() normalizes all three without a dynamic bind
        // or a hard dependency on either JSON library.
        var serviceHashCode = schemaHealthCheck.Data.TryGetValue(SchemaHealthCheckConstants.HashCodeKey, out var raw)
            ? raw?.ToString()
            : null;

        var isMatch = serviceHashCode != null && hashCode == serviceHashCode;

        // Copy the data (don't mutate the caller's result) and record the verdict.
        var data = new Dictionary<string, object>(schemaHealthCheck.Data)
        {
            [SchemaHealthCheckConstants.MatchKey] = new ClientHashMatch
            {
                ServiceHashCode = serviceHashCode,
                ClientHashCode = hashCode,
                IsMatch = isMatch,
            },
        };

        // Genuine drift (both hashes present and differing) degrades the schema check to Warning so a
        // health consumer sees drift as a first-class status, not only buried in Data. Warning does not
        // flip the aggregate IsHealthy (drift is degraded-but-not-fatal), and a check that already
        // reports Warning/Failed - or that has no hash to compare against - keeps its own status.
        var status = serviceHashCode != null && !isMatch && schemaHealthCheck.Status == HealthCheckStatus.Ok
            ? HealthCheckStatus.Warning
            : schemaHealthCheck.Status;

        var annotated = new HealthCheckResult(status, schemaHealthCheck.Type, data, schemaHealthCheck.Dependencies);

        var healthChecks = healthCheckResponse.HealthChecks.ToDictionary(x => x.Key, x => x.Value);
        healthChecks[entry.Key] = annotated;

        return new HealthCheckResponse(healthCheckResponse.IsHealthy, healthChecks);
    }
}
