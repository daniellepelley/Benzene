using Azure.Identity;
using Azure.Monitor.Query;
using Benzene.Abstractions.DI;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Usage.ApplicationInsights;

/// <summary>
/// Provides extension methods for registering the Application Insights usage source with a Benzene
/// service container.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers an <see cref="ApplicationInsightsUsageSource"/> as an <see cref="IMeshUsageSource"/>, so
    /// <c>Benzene.Mesh.Aggregator</c> reads the <c>benzene.messages.processed</c> counter back from the
    /// Application Insights / Log Analytics workspace each run and merges it into <c>usage.json</c>.
    /// </summary>
    /// <remarks>
    /// Registers a default <see cref="LogsQueryClient"/> authenticated with <see cref="DefaultAzureCredential"/>
    /// (which resolves the ambient managed identity on Azure) and the default KQL query seam, unless the
    /// caller has already registered its own. Requires
    /// <c>Benzene.Mesh.Aggregator.Extensions.AddMeshAggregator(...)</c> in the same container - the aggregator
    /// resolves every registered source per run.
    /// </remarks>
    /// <param name="services">The service container to register with.</param>
    /// <param name="options">Which workspace/metric to read, and over what window.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddApplicationInsightsUsage(
        this IBenzeneServiceContainer services, ApplicationInsightsUsageOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton(_ => new LogsQueryClient(new DefaultAzureCredential()));
        services.AddSingleton<IApplicationInsightsUsageQuery, LogsQueryUsageQuery>();
        services.AddSingleton<IMeshUsageSource, ApplicationInsightsUsageSource>();
        return services;
    }
}
