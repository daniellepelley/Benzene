namespace Benzene.Mesh.Contracts;

/// <summary>
/// The top-level index a <c>Benzene.Mesh.Aggregator</c> publishes on each run - the
/// <c>manifest.json</c> shape. A catalog/topology UI reads this first, then drills into individual
/// <see cref="MeshServiceSnapshot"/> artifacts as needed.
/// </summary>
public class MeshManifest
{
    /// <summary>Initializes a new instance of the <see cref="MeshManifest"/> class.</summary>
    /// <param name="generatedAtUtc">When this manifest was generated.</param>
    /// <param name="services">One entry per registered service.</param>
    public MeshManifest(DateTimeOffset generatedAtUtc, MeshManifestEntry[] services)
    {
        GeneratedAtUtc = generatedAtUtc;
        Services = services;
    }

    /// <summary>When this manifest was generated.</summary>
    public DateTimeOffset GeneratedAtUtc { get; }

    /// <summary>One entry per registered service.</summary>
    public MeshManifestEntry[] Services { get; }
}
