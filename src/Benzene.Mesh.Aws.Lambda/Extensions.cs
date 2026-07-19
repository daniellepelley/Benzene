using Amazon.Lambda;
using Benzene.Abstractions.DI;
using Benzene.Clients.Aws.Lambda;
using Benzene.Mesh.Aggregator;

namespace Benzene.Mesh.Aws.Lambda;

/// <summary>
/// Provides extension methods for registering the AWS Lambda Invoke mesh source with a Benzene
/// service container.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers <see cref="LambdaMeshServiceSource"/> (as an additional <see cref="IMeshServiceSource"/>)
    /// against a default-credential-chain <see cref="AmazonLambdaClient"/>.
    /// </summary>
    /// <remarks>
    /// Requires <c>Benzene.Mesh.Aggregator.Extensions.AddMeshAggregator(...)</c> to already be
    /// registered in the same container - <see cref="MeshAggregator"/> itself is registered there,
    /// and resolves every <see cref="IMeshServiceSource"/> (including this one) via
    /// <c>IEnumerable&lt;IMeshServiceSource&gt;</c> constructor injection.
    /// </remarks>
    /// <param name="services">The service container to register with.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddMeshLambdaSource(this IBenzeneServiceContainer services)
    {
        services.AddSingleton<IAmazonLambda>(_ => new AmazonLambdaClient());
        services.AddSingleton<IAwsLambdaClient>(resolver => new AwsLambdaClient(resolver.GetService<IAmazonLambda>()));
        // Register the source with a *lazy* client handle: MeshAggregator resolves every
        // IMeshServiceSource eagerly at startup, so constructing the client here would force an
        // AmazonLambdaClient (which needs a region) even for a pure-HTTP mesh. Deferring it means the
        // AWS client is only built the first time a service with Source=AwsLambdaInvoke is fetched.
        services.AddSingleton<IMeshServiceSource>(resolver =>
            new LambdaMeshServiceSource(new Lazy<IAwsLambdaClient>(resolver.GetService<IAwsLambdaClient>)));
        return services;
    }
}
