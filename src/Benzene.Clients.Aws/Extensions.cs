using System;
using Amazon.Lambda;
using Amazon.SQS;
using Amazon.StepFunctions;
using Benzene.Abstractions.DI;
using Benzene.Clients.Aws.Lambda;
using Benzene.Clients.Aws.Sqs;
using Benzene.Clients.Aws.StepFunctions;
using Benzene.HealthChecks.Core;
using Microsoft.Extensions.Logging;

namespace Benzene.Clients.Aws;

/// <summary>
/// Provides top-level extension methods for registering AWS-backed Benzene message clients and health checks.
/// </summary>
public static class Extensions
{
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
        return builder.AddHealthCheck(resolver => new AwsLambdaHealthCheck(lambdaName, resolver.GetService<IAmazonLambda>(), resolver.GetService<ILogger<AwsLambdaHealthCheck>>()));
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
