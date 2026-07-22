using Amazon.CloudWatch;
using Benzene.Abstractions.DI;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Usage.CloudWatch;

/// <summary>
/// Provides extension methods for registering the CloudWatch usage source with a Benzene service container.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers a <see cref="CloudWatchUsageSource"/> as an <see cref="IMeshUsageSource"/>, so
    /// <c>Benzene.Mesh.Aggregator</c> reads the <c>benzene.messages.processed</c> counter back from
    /// CloudWatch each run and merges it into <c>usage.json</c>.
    /// </summary>
    /// <remarks>
    /// Registers a default <see cref="IAmazonCloudWatch"/> (which resolves region and credentials from the
    /// ambient AWS environment - on Lambda, the execution role) unless the caller has already registered
    /// one. Requires <c>Benzene.Mesh.Aggregator.Extensions.AddMeshAggregator(...)</c> in the same container,
    /// the same additive pattern as any other <see cref="IMeshUsageSource"/> - the aggregator resolves every
    /// registered source per run.
    /// </remarks>
    /// <param name="services">The service container to register with.</param>
    /// <param name="options">Which CloudWatch metric to read, and over what window.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddCloudWatchUsage(
        this IBenzeneServiceContainer services, CloudWatchUsageOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IAmazonCloudWatch>(_ => new AmazonCloudWatchClient());
        services.AddSingleton<IMeshUsageSource, CloudWatchUsageSource>();
        return services;
    }
}
