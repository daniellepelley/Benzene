namespace Benzene.HealthChecks.Core;

/// <summary>
/// The wire contract shared between a service's provider-side schema health check
/// (<c>Benzene.HealthChecks.Schema.SchemaHealthCheck</c>) and the consumer-side comparison
/// (<c>Benzene.Clients.HealthChecks.ClientHealthCheckProcessor</c>): the health check's
/// <see cref="IHealthCheckResult.Type"/> and the <see cref="IHealthCheckResult.Data"/> key its
/// contract hash is published under. Centralized here so the two ends can't drift on a string literal.
/// </summary>
public static class SchemaHealthCheckConstants
{
    /// <summary>The <see cref="IHealthCheckResult.Type"/> a schema/contract health check reports under.</summary>
    public const string Type = "schema";

    /// <summary>The <see cref="IHealthCheckResult.Data"/> key the contract hash is published under (a plain string, so it survives any JSON round-trip).</summary>
    public const string HashCodeKey = "hashCode";

    /// <summary>The <see cref="IHealthCheckResult.Data"/> key the consumer writes its hash-match verdict under after comparing.</summary>
    public const string MatchKey = "match";
}
