using Amazon.StepFunctions;
using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Aws.StepFunctions;

/// <summary>
/// Provides extension methods for registering AWS Step Functions health checks.
/// </summary>
public static class Extensions
{
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
