using System;
using Amazon.Lambda;
using Amazon.SQS;
using Amazon.StepFunctions;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Clients.Aws.Lambda;
using Benzene.Clients.Aws.Sqs;
using Benzene.Clients.Aws.StepFunctions;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Aws;

/// <summary>
/// Provides top-level extension methods for registering AWS-backed Benzene message clients and health checks.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers a set of named Benzene message clients using a <see cref="ClientsBuilder"/>.
    /// </summary>
    /// <param name="services">The service container to register clients with.</param>
    /// <param name="builder">The action that configures the clients builder (e.g. by calling
    /// <c>CreateSqsBenzeneMessageClient</c> or <c>CreateAwsLambdaBenzeneMessageClient</c>).</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddBenzeneMessageClients(this IBenzeneServiceContainer services, Action<ClientsBuilder> builder)
    {
        var clientsBuilder = new ClientsBuilder(services);
        builder(clientsBuilder);
        // clientsBuilder.Register(services);
        return services;
    }

    /// <summary>
    /// Registers a single Benzene message client using a <see cref="SingleClientsBuilder"/>.
    /// </summary>
    /// <param name="services">The service container to register the client with.</param>
    /// <param name="builder">The action that configures the client builder.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddBenzeneMessageClient(this IBenzeneServiceContainer services, Action<SingleClientsBuilder> builder)
    {
        var clientsBuilder = new SingleClientsBuilder();
        builder(clientsBuilder);
        clientsBuilder.Register(services);
        return services;
    }

    /// <summary>
    /// Registers the services required to send messages via AWS Lambda invocation, including retry and
    /// sender-header decoration.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <param name="sender">The sender name added as a header to every outgoing message.</param>
    /// <returns>The service container for method chaining.</returns>
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

    /// <summary>
    /// Adds a health check that pings an SQS queue.
    /// </summary>
    /// <param name="builder">The health check builder to add the check to.</param>
    /// <param name="queueUrl">The URL of the queue to ping.</param>
    /// <returns>The health check builder for method chaining.</returns>
    public static IHealthCheckBuilder AddSqsHealthCheck(this IHealthCheckBuilder builder, string queueUrl)
    {
        return builder.AddHealthCheck(resolver => new SqsHealthCheck(queueUrl, resolver.GetService<IAmazonSQS>()));
    }

    /// <summary>
    /// Adds a health check that pings an AWS Lambda function.
    /// </summary>
    /// <param name="builder">The health check builder to add the check to.</param>
    /// <param name="lambdaName">The name of the Lambda function to ping.</param>
    /// <returns>The health check builder for method chaining.</returns>
    public static IHealthCheckBuilder AddLambdaHealthCheck(this IHealthCheckBuilder builder, string lambdaName)
    {
        return builder.AddHealthCheck(resolver => new AwsLambdaHealthCheck(lambdaName, resolver.GetService<IAmazonLambda>(), resolver.GetService<IBenzeneLogger>()));
    }

    /// <summary>
    /// Adds a health check that starts a Step Functions state machine execution.
    /// </summary>
    /// <param name="builder">The health check builder to add the check to.</param>
    /// <param name="stateMachineArn">The ARN of the state machine to ping.</param>
    /// <returns>The health check builder for method chaining.</returns>
    public static IHealthCheckBuilder AddStepFunctionHealthCheck(this IHealthCheckBuilder builder, string stateMachineArn)
    {
        return builder.AddHealthCheck(resolver => new StepFunctionsHealthCheck(stateMachineArn, resolver.GetService<IAmazonStepFunctions>()));
    }
}
