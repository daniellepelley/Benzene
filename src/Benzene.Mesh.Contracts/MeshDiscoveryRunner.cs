namespace Benzene.Mesh.Contracts;

/// <summary>
/// Runs the discovery phase: executes every registered <see cref="IMeshDiscoveryProvider"/>, unions
/// the results with an optional hand-written static seed, de-duplicates by service name, and returns
/// the <see cref="MeshServiceRegistry"/> the aggregator will consume. This is the "discovery creates
/// the config" step — decoupled from the runtime poll loop so the two can be hosted and scheduled
/// independently.
/// </summary>
public class MeshDiscoveryRunner
{
    private readonly IReadOnlyList<IMeshDiscoveryProvider> _providers;

    /// <summary>Initializes the runner over the registered providers.</summary>
    /// <param name="providers">The discovery providers to run (mirrors the multi-source DI pattern).</param>
    public MeshDiscoveryRunner(IEnumerable<IMeshDiscoveryProvider> providers)
    {
        _providers = providers.ToArray();
    }

    /// <summary>
    /// Discovers services and merges them with <paramref name="staticSeed"/>. On a name clash the
    /// seed (and then an earlier provider) wins — a hand-pinned entry is an intentional human override
    /// that discovery must not silently replace.
    /// </summary>
    /// <param name="filter">The discovery filter passed to every provider.</param>
    /// <param name="staticSeed">Optional hand-written registry to union in (wins on a name clash).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task<MeshServiceRegistry> DiscoverAsync(
        MeshDiscoveryFilter filter,
        MeshServiceRegistry? staticSeed = null,
        CancellationToken cancellationToken = default)
    {
        var byName = new Dictionary<string, MeshServiceRegistryEntry>(StringComparer.OrdinalIgnoreCase);

        if (staticSeed != null)
        {
            foreach (var entry in staticSeed.Services)
            {
                byName[entry.Name] = entry;
            }
        }

        foreach (var provider in _providers)
        {
            var discovered = await provider.DiscoverAsync(filter, cancellationToken);
            foreach (var entry in discovered)
            {
                if (!byName.ContainsKey(entry.Name))
                {
                    byName[entry.Name] = entry;
                }
            }
        }

        return new MeshServiceRegistry(byName.Values.ToArray());
    }
}
