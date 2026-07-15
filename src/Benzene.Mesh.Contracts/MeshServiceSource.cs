namespace Benzene.Mesh.Contracts;

/// <summary>
/// Known values for <see cref="MeshServiceRegistryEntry.Source"/> - which
/// <c>Benzene.Mesh.Aggregator.IMeshServiceSource</c> a registry entry should be fetched with.
/// New adapter-package sources (e.g. an AWS Lambda Invoke adapter) get their constant added here
/// rather than defined as a private literal in the adapter package itself, so registry config
/// authors have one place to look up valid values - this class has no dependency on the adapter
/// packages themselves, it's just the shared "known names" list.
/// </summary>
public static class MeshServiceSource
{
    /// <summary>Fetch over HTTP, using <see cref="MeshServiceRegistryEntry.SpecUrl"/>/<see cref="MeshServiceRegistryEntry.HealthUrl"/> - the default.</summary>
    public const string Http = "Http";
}
