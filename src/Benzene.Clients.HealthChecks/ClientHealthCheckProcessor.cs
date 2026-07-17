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
        var schemaHealthCheck = healthCheckResponse.HealthChecks
            .FirstOrDefault(x => x.Value.Type == SchemaHealthCheckConstants.Type).Value;

        // No schema health check to compare against - nothing to annotate, pass the response through.
        if (schemaHealthCheck == null)
        {
            return new HealthCheckResponse(healthCheckResponse.IsHealthy, healthCheckResponse.HealthChecks);
        }

        // The hash is published as a plain string, but after the response is serialized over the wire
        // and deserialized it may arrive boxed as a JsonElement (System.Text.Json) or a JToken
        // (Newtonsoft) rather than a string - ToString() normalizes all three without a dynamic bind
        // or a hard dependency on either JSON library.
        var serviceHashCode = schemaHealthCheck.Data.TryGetValue(SchemaHealthCheckConstants.HashCodeKey, out var raw)
            ? raw?.ToString()
            : null;

        var isMatch = serviceHashCode != null && hashCode == serviceHashCode;

        schemaHealthCheck.Data[SchemaHealthCheckConstants.MatchKey] = new ClientHashMatch
        {
            ServiceHashCode = serviceHashCode,
            ClientHashCode = hashCode,
            IsMatch = isMatch,
        };

        return new HealthCheckResponse(healthCheckResponse.IsHealthy, healthCheckResponse.HealthChecks);
    }
}
