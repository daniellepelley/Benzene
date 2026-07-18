using Amazon.Lambda;
using Benzene.Abstractions.DI;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Discovery.Aws;

/// <summary>Registration for AWS Lambda mesh discovery.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers <see cref="AwsLambdaDiscoveryProvider"/> (as an additional
    /// <see cref="IMeshDiscoveryProvider"/>) against a default-credential-chain
    /// <see cref="AmazonLambdaClient"/>, plus a <see cref="MeshDiscoveryRunner"/> over all registered
    /// providers. Mirrors <c>Benzene.Mesh.Aws.Lambda.AddMeshLambdaSource</c>'s DI shape.
    /// </summary>
    /// <param name="services">The service container to register with.</param>
    public static IBenzeneServiceContainer AddMeshAwsLambdaDiscovery(this IBenzeneServiceContainer services)
    {
        services.AddSingleton<IAmazonLambda>(_ => new AmazonLambdaClient());
        services.AddSingleton<IMeshDiscoveryProvider>(resolver =>
            new AwsLambdaDiscoveryProvider(resolver.GetService<IAmazonLambda>()));
        services.AddSingleton(resolver => new MeshDiscoveryRunner(resolver.GetServices<IMeshDiscoveryProvider>()));
        return services;
    }
}
