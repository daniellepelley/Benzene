namespace Benzene.Clients.HealthChecks;

/// <summary>
/// The verdict of comparing a consumer's baked-in contract hash against the provider's live contract
/// hash. Written into the schema health check's data by <see cref="ClientHealthCheckProcessor"/>.
/// </summary>
public class ClientHashMatch
{
    /// <summary>Gets or sets the provider's current contract hash.</summary>
    public string? ServiceHashCode { get; set; }

    /// <summary>Gets or sets the hash the consumer's client was generated against.</summary>
    public string? ClientHashCode { get; set; }

    /// <summary>Gets or sets whether the two hashes match (i.e. no contract drift).</summary>
    public bool IsMatch { get; set; }
}
