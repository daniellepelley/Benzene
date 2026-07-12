using System;
using Amazon.Lambda;
using Microsoft.Extensions.Logging;

namespace Benzene.Clients.Aws.Lambda;

/// <summary>
/// Provides extension methods for registering <see cref="AwsLambdaBenzeneMessageClient"/> instances on a
/// <see cref="ClientsBuilder"/>.
/// </summary>
public static class AwsLambdaBenzeneMessageClientExtensions
{
    /// <summary>
    /// Registers an <see cref="AwsLambdaBenzeneMessageClient"/> targeting the given Lambda function, with
    /// optional service/topic mapping and client build configuration.
    /// </summary>
    /// <param name="source">The clients builder to register on.</param>
    /// <param name="lambdaName">The name of the target Lambda function.</param>
    /// <param name="mapClient">An optional callback used to configure which service/topic combinations map to this client.</param>
    /// <param name="buildClient">An optional callback used to further configure the created client builder.</param>
    /// <returns>The clients builder, for chaining.</returns>
    public static ClientsBuilder CreateAwsLambdaBenzeneMessageClient(this ClientsBuilder source, string lambdaName, Action<ClientMappingBuilder> mapClient = null, Action<ClientBuilder> buildClient =
        null)
    {
        var clientBuilder = new ClientBuilder(resolver =>
            new AwsLambdaBenzeneMessageClient(lambdaName, resolver.GetService<IAmazonLambda>(), resolver.GetService<ILogger<AwsLambdaBenzeneMessageClient>>()));

        if (buildClient != null)
        {
            buildClient(clientBuilder);
        }

        var clientMappingBuilder = new ClientMappingBuilder();
        if (mapClient != null)
        {
            mapClient(clientMappingBuilder);
        }

        source.WithMessageClient(new ClientMapping(clientMappingBuilder.Build(), clientBuilder.Build));

        return source;
    }
}
