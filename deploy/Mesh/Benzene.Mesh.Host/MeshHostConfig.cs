using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Host;

/// <summary>
/// The config-bound shape of <c>mesh.json</c> (or equivalent environment variables, via .NET's
/// standard double-underscore binding, e.g. <c>Services__0__Name</c>) - this repo's first use of
/// <c>IConfiguration.Get&lt;T&gt;()</c> binding a list of objects, flagged in
/// <c>work/service-mesh-roadmap-1.0.md</c> as genuinely new territory, not an established Benzene
/// convention being reused. Mutable properties (not the constructor-based immutable pattern the
/// rest of <c>Benzene.Mesh.Contracts</c> uses) are required for the configuration binder.
/// </summary>
public class MeshHostConfig
{
    /// <summary>Where generated catalog artifacts are written - bind-mount a volume here for persistence across container restarts.</summary>
    public string ArtifactRootDirectory { get; set; } = "mesh-artifacts";

    /// <summary>How often the background poll loop runs a full aggregation pass.</summary>
    public int PollIntervalSeconds { get; set; } = 60;

    /// <summary>The services to poll each pass.</summary>
    public MeshHostServiceConfig[] Services { get; set; } = Array.Empty<MeshHostServiceConfig>();
}

/// <summary>One <c>mesh.json</c> service entry, converted to a <see cref="MeshServiceRegistryEntry"/> via <see cref="ToEntry"/>.</summary>
public class MeshHostServiceConfig
{
    /// <summary>The service's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The service's spec URL - required for <see cref="MeshServiceSource.Http"/>, optional (display-only) for other sources.</summary>
    public string? SpecUrl { get; set; }

    /// <summary>The service's health URL - required for <see cref="MeshServiceSource.Http"/>, optional (display-only) for other sources.</summary>
    public string? HealthUrl { get; set; }

    /// <summary>Which <c>IMeshServiceSource</c> fetches this entry - see <see cref="MeshServiceSource"/>. Defaults to <see cref="MeshServiceSource.Http"/>.</summary>
    public string Source { get; set; } = MeshServiceSource.Http;

    /// <summary>Source-specific configuration (e.g. <c>{"functionName": "...", "region": "..."}"</c> for <see cref="MeshServiceSource.AwsLambdaInvoke"/>).</summary>
    public Dictionary<string, string>? SourceOptions { get; set; }

    /// <summary>Converts this config entry to the registry shape <see cref="Benzene.Mesh.Aggregator.MeshAggregator"/> consumes.</summary>
    public MeshServiceRegistryEntry ToEntry() => new(Name, SpecUrl ?? string.Empty, HealthUrl ?? string.Empty, Source, SourceOptions);
}
