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
    /// Adds a Step Functions state machine health check. By default (<see cref="HealthCheckMode.Reachability"/>)
    /// this is a non-destructive read-only <c>DescribeStateMachine</c> probe; pass
    /// <see cref="HealthCheckMode.Active"/> to start a real execution instead (side-effecting).
    /// </summary>
    /// <param name="builder">The health check builder to add the check to.</param>
    /// <param name="stateMachineArn">The ARN of the state machine to check.</param>
    /// <param name="mode">Reachability (default, read-only) or Active (starts an execution — side-effecting).</param>
    /// <returns>The health check builder for method chaining.</returns>
    public static IHealthCheckBuilder AddStepFunctionHealthCheck(this IHealthCheckBuilder builder, string stateMachineArn, HealthCheckMode mode = HealthCheckMode.Reachability)
    {
        return builder.AddHealthCheck(resolver => new StepFunctionsHealthCheck(stateMachineArn, resolver.GetService<IAmazonStepFunctions>(), mode));
    }
}
