using Benzene.Abstractions.DI;

namespace Benzene.Mesh.Tracing.Tempo;

/// <summary>
/// Provides extension methods for registering the Tempo topology adapter with a Benzene service container.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers <see cref="TempoServiceGraphTopologyBuilder"/>, backed by a
    /// <see cref="PrometheusQueryClient"/>, against the given <paramref name="options"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately does not register an <c>IMeshArtifactStore</c> - this requires
    /// <c>Benzene.Mesh.Aggregator.Extensions.AddMeshAggregator(...)</c> to already be registered
    /// in the same container, so <c>topology.json</c> is published to the same artifact directory
    /// <c>manifest.json</c>/<c>services/*.json</c> are. Resolving
    /// <see cref="TempoTopologyMessageHandler"/> without that prerequisite fails at DI-resolution
    /// time, the same way any other Benzene wiring with a missing prerequisite registration would.
    /// </remarks>
    /// <param name="services">The service container to register with.</param>
    /// <param name="options">Where and over what window to query Tempo's service-graph metrics.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddTempoTopology(
        this IBenzeneServiceContainer services, TempoTopologyOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<HttpClient>();
        services.AddSingleton<PrometheusQueryClient>();
        services.AddSingleton<TempoServiceGraphTopologyBuilder>();
        return services;
    }
}
