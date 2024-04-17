using System;
using Amazon.Lambda;
using Amazon.SQS;
using Amazon.StepFunctions;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Clients.Aws.Lambda;
using Benzene.Clients.Aws.Sqs;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Aws;

public static class Extensions
{
    public static IBenzeneServiceContainer AddBenzeneMessageClients(this IBenzeneServiceContainer services, Action<ClientsBuilder> builder)
    {
        var clientsBuilder = new ClientsBuilder(services);
        builder(clientsBuilder);
        // clientsBuilder.Register(services);
        return services;
    }
    
    public static IBenzeneServiceContainer AddBenzeneMessageClient(this IBenzeneServiceContainer services, Action<SingleClientsBuilder> builder)
    {
        var clientsBuilder = new SingleClientsBuilder();
        builder(clientsBuilder);
        clientsBuilder.Register(services);
        return services;
    }

    public static IBenzeneServiceContainer AddLambdaClients(this IBenzeneServiceContainer services, string sender)
    {
        services.AddScoped<AwsLambdaBenzeneMessageClient>();
        services.AddScoped(x => new RetryBenzeneMessageClient(x.GetService<AwsLambdaBenzeneMessageClient>()));
        services.AddScoped(x =>
            new HeaderBenzeneMessageClient(x.GetService<RetryBenzeneMessageClient>(), "sender", sender));
        services.AddScoped<IBenzeneMessageClient>(x =>
            new HeadersBenzeneMessageClient(x.GetService<HeaderBenzeneMessageClient>(), x.GetService<IClientHeaders>()));
        services.AddScoped<IBenzeneMessageClientFactory, AwsLambdaBenzeneMessageClientFactory>();
        services.AddScoped<IAwsLambdaClient, AwsLambdaClient>();
        services.AddScoped<IClientHeaders, ClientHeaders>();
        return services;
    }

    public static IHealthCheckBuilder AddSqsHealthCheck(this IHealthCheckBuilder builder, string queueUrl)
    {
        return builder.AddHealthCheck(resolver => new SqsHealthCheck(queueUrl, resolver.GetService<IAmazonSQS>()));
    }
    
    public static IHealthCheckBuilder AddLambdaHealthCheck(this IHealthCheckBuilder builder, string lambdaName)
    {
        return builder.AddHealthCheck(resolver => new AwsLambdaHealthCheck(lambdaName, resolver.GetService<IAmazonLambda>(), resolver.GetService<IBenzeneLogger>()));
    }

    public static IHealthCheckBuilder AddStepFunctionHealthCheck(this IHealthCheckBuilder builder, string stateMachineArn)
    {
        return builder.AddHealthCheck(resolver => new StepFunctionsHealthCheck(stateMachineArn, resolver.GetService<IAmazonStepFunctions>()));
    }

}
