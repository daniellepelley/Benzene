namespace Benzene.Mesh.Contracts;

/// <summary>
/// One row in a <see cref="MeshManifest"/> - a summary of one service's latest snapshot, denormalized
/// so a catalog view can render service status without fetching every service's full
/// <see cref="MeshServiceSnapshot"/>.
/// </summary>
public class MeshManifestEntry
{
    /// <summary>Initializes a new instance of the <see cref="MeshManifestEntry"/> class.</summary>
    /// <param name="name">The service's name.</param>
    /// <param name="status">One of <see cref="MeshServiceStatus"/>.</param>
    /// <param name="contractDrift">Whether this service's spec changed since the previous run.</param>
    /// <param name="specUrl">The URL the spec was fetched from.</param>
    /// <param name="healthUrl">The URL the health check response was fetched from.</param>
    /// <param name="owningTeam">The team or individual to contact about this service, or <c>null</c> if unset.</param>
    /// <param name="transports">
    /// The names of every transport this service is wired to receive messages over (e.g.
    /// <c>["sqs", "http"]</c>), lifted from its spec's top-level <c>transports</c> field. Empty
    /// when the spec didn't advertise any (an older service, or a spec fetch failure) - denormalized
    /// here, same as <paramref name="owningTeam"/>, so a catalog view can render it without fetching
    /// every service's full snapshot.
    /// </param>
    /// <param name="snapshotAtUtc">
    /// When this service's underlying <see cref="MeshServiceSnapshot"/> was taken
    /// (<see cref="MeshServiceSnapshot.FetchedAtUtc"/>), denormalized here so a catalog/issue view can
    /// judge freshness from <c>manifest.json</c> alone without fetching every snapshot. <c>null</c> on
    /// a manifest written before this field existed. Distinct from <see cref="MeshManifest.GeneratedAtUtc"/>
    /// — in push/self-report mode a single row's snapshot can be older than the run that emitted the
    /// manifest, which is exactly what makes a "stale service" detectable.
    /// </param>
    public MeshManifestEntry(string name, string status, bool contractDrift, string specUrl, string healthUrl,
        string? owningTeam = null, string[]? transports = null, DateTimeOffset? snapshotAtUtc = null)
    {
        Name = name;
        Status = status;
        ContractDrift = contractDrift;
        SpecUrl = specUrl;
        HealthUrl = healthUrl;
        OwningTeam = owningTeam;
        Transports = transports ?? Array.Empty<string>();
        SnapshotAtUtc = snapshotAtUtc;
    }

    /// <summary>The service's name.</summary>
    public string Name { get; }

    /// <summary>One of <see cref="MeshServiceStatus"/>.</summary>
    public string Status { get; }

    /// <summary>Whether this service's spec changed since the previous run.</summary>
    public bool ContractDrift { get; }

    /// <summary>The URL the spec was fetched from.</summary>
    public string SpecUrl { get; }

    /// <summary>The URL the health check response was fetched from.</summary>
    public string HealthUrl { get; }

    /// <summary>The team or individual to contact about this service, or <c>null</c> if unset.</summary>
    public string? OwningTeam { get; }

    /// <summary>
    /// The names of every transport this service is wired to receive messages over, or an empty
    /// array if its spec didn't advertise any.
    /// </summary>
    public string[] Transports { get; }

    /// <summary>
    /// When this service's underlying snapshot was taken, denormalized from
    /// <see cref="MeshServiceSnapshot.FetchedAtUtc"/> so freshness can be judged from the manifest
    /// alone. <c>null</c> on a manifest written before this field existed. Distinct from
    /// <see cref="MeshManifest.GeneratedAtUtc"/>.
    /// </summary>
    public DateTimeOffset? SnapshotAtUtc { get; }
}
