﻿using System;
using Amazon.SQS;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;

namespace Benzene.Clients.Aws.Sqs;

public static class SqsBenzeneMessageClientExtensions
{
    public static ClientsBuilder CreateSqsBenzeneMessageClient(this ClientsBuilder source, string name, string queueUrl, IServiceResolver serviceResolver, Action<ClientBuilder> action)
    {
        var clientBuilder = new ClientBuilder(resolver =>
            new SqsBenzeneMessageClient(queueUrl, resolver.GetService<IAmazonSQS>(), resolver.GetService<IBenzeneLogger>(), serviceResolver));

        action(clientBuilder);
        source.WithMessageClient(name, clientBuilder.Build);
            
        return source;
    }
    
    public static void CreateSqsBenzeneMessageClient(this ClientsBuilder source, string queueUrl, IServiceResolver serviceResolver, Action<ClientBuilder> action)
    {
        var clientBuilder = new ClientBuilder(resolver =>
            new SqsBenzeneMessageClient(queueUrl, resolver.GetService<IAmazonSQS>(), resolver.GetService<IBenzeneLogger>(), serviceResolver));

        action(clientBuilder);
        source.WithMessageClient(string.Empty, clientBuilder.Build);
    }
}
