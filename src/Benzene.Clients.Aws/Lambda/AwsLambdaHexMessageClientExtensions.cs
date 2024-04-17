using System;
using Amazon.Lambda;
using Benzene.Abstractions.Logging;

namespace Benzene.Clients.Aws.Lambda;

public static class AwsLambdaBenzeneMessageClientExtensions
{
    public static ClientsBuilder CreateAwsLambdaBenzeneMessageClient(this ClientsBuilder source, string lambdaName, Action<ClientMappingBuilder> mapClient = null, Action<ClientBuilder> buildClient =
        null)
    {
        var clientBuilder = new ClientBuilder(resolver =>
            new AwsLambdaBenzeneMessageClient(lambdaName, resolver.GetService<IAmazonLambda>(), resolver.GetService<IBenzeneLogger>()));

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
