using Amazon.StepFunctions;
using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;
using Microsoft.Extensions.Logging;

namespace Benzene.Clients.Aws.StepFunctions;

/// <summary>
/// Provides extension methods for registering the AWS Step Functions client and its health check.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers an <see cref="IStepFunctionsClientFactory"/> / <see cref="IStepFunctionsClient"/> for a
    /// fixed state machine, resolving <see cref="IAmazonStepFunctions"/> from the container (the consumer
    /// registers it). This is the DI-registration seam Step Functions previously lacked — the client was
    /// only constructible by hand — and it is where the health check auto-wires from.
    /// </summary>
    /// <param name="services">The service container to register on.</param>
    /// <param name="stateMachineArn">The ARN of the state machine the client targets.</param>
    /// <param name="healthCheck">
    /// When <c>true</c> (the default) a non-destructive Step Functions reachability check
    /// (<c>DescribeStateMachine</c>) for <paramref name="stateMachineArn"/> is auto-registered on the deep
    /// <c>healthcheck</c> layer — never a Kubernetes probe (see <see cref="IDependencyHealthCheck"/>). Pass
    /// <c>false</c> to opt out.
    /// </param>
    /// <returns>The service container, for chaining.</returns>
    public static IBenzeneServiceContainer AddStepFunctionsClient(this IBenzeneServiceContainer services, string stateMachineArn, bool healthCheck = true)
    {
        services.AddScoped<IStepFunctionsClientFactory>(resolver => new StepFunctionsClientFactory(
            stateMachineArn, resolver.GetService<IAmazonStepFunctions>(), resolver.GetService<ILogger<StepFunctionsClient>>()));
        services.AddScoped(resolver => resolver.GetService<IStepFunctionsClientFactory>().Create());

        if (healthCheck)
        {
            services.AddDependencyHealthCheck(
                resolver => new StepFunctionsHealthCheck(stateMachineArn, resolver.GetService<IAmazonStepFunctions>()),
                $"StepFunctions:{stateMachineArn}");
        }

        return services;
    }

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
