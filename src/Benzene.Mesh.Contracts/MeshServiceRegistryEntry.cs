namespace Benzene.Mesh.Contracts;

/// <summary>
/// One entry in a <see cref="MeshServiceRegistry"/> - a human-maintained record of where to find a
/// service's spec and health endpoints. Not generated; this is the input a
/// <c>Benzene.Mesh.Aggregator</c> polls.
/// </summary>
public class MeshServiceRegistryEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MeshServiceRegistryEntry"/> class, fetched over
    /// HTTP (<see cref="MeshServiceSource.Http"/>).
    /// </summary>
    /// <param name="name">The service's name, used as its key across all generated mesh artifacts.</param>
    /// <param name="specUrl">The URL to fetch the service's spec document from (e.g. <c>https://.../spec?type=benzene</c>).</param>
    /// <param name="healthUrl">The URL to fetch the service's aggregated health check response from.</param>
    public MeshServiceRegistryEntry(string name, string specUrl, string healthUrl)
        : this(name, specUrl, healthUrl, MeshServiceSource.Http, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MeshServiceRegistryEntry"/> class, fetched using
    /// a specific <paramref name="source"/> (see <see cref="MeshServiceSource"/>).
    /// </summary>
    /// <param name="name">The service's name, used as its key across all generated mesh artifacts.</param>
    /// <param name="specUrl">
    /// The URL to fetch the service's spec document from (e.g. <c>https://.../spec?type=benzene</c>).
    /// Only meaningful for HTTP-fetched sources - other sources may still carry a human-readable
    /// value here purely for the manifest/UI to link out to, even when the fetch itself doesn't use it.
    /// </param>
    /// <param name="healthUrl">
    /// The URL to fetch the service's aggregated health check response from. Same caveat as <paramref name="specUrl"/>.
    /// </param>
    /// <param name="source">Which <c>Benzene.Mesh.Aggregator.IMeshServiceSource</c> fetches this entry - see <see cref="MeshServiceSource"/>.</param>
    /// <param name="sourceOptions">
    /// Source-specific configuration (e.g. an AWS Lambda function name/region). Deliberately an
    /// untyped string dictionary rather than typed per-source subclasses, so this package doesn't
    /// need to know what every adapter package requires.
    /// </param>
    public MeshServiceRegistryEntry(string name, string specUrl, string healthUrl, string source, IReadOnlyDictionary<string, string>? sourceOptions)
    {
        Name = name;
        SpecUrl = specUrl;
        HealthUrl = healthUrl;
        Source = source;
        SourceOptions = sourceOptions;
    }

    /// <summary>The service's name, used as its key across all generated mesh artifacts.</summary>
    public string Name { get; }

    /// <summary>The URL to fetch the service's spec document from (e.g. <c>https://.../spec?type=benzene</c>).</summary>
    public string SpecUrl { get; }

    /// <summary>The URL to fetch the service's aggregated health check response from.</summary>
    public string HealthUrl { get; }

    /// <summary>Which <c>Benzene.Mesh.Aggregator.IMeshServiceSource</c> fetches this entry. Defaults to <see cref="MeshServiceSource.Http"/>.</summary>
    public string Source { get; }

    /// <summary>Source-specific configuration, or <c>null</c> if the source needs none.</summary>
    public IReadOnlyDictionary<string, string>? SourceOptions { get; }
}
