using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.HealthChecks;

/// <summary>
/// Registration helpers for the consumer-side contract-drift check (<see cref="ClientHealthCheck"/>).
/// Add these to the <em>contracts</em> diagnostic topic via <c>UseContractsCheck(...)</c> - never a
/// liveness or readiness probe (see <see cref="ClientHealthCheck"/> and
/// <c>docs/kubernetes-health-checks.md</c>).
/// </summary>
public static class ContractHealthCheckExtensions
{
    /// <summary>
    /// Registers a <see cref="ClientHealthCheck"/> for a downstream service, resolving its generated
    /// client <typeparamref name="TClient"/> from DI each time checks run.
    /// </summary>
    /// <typeparam name="TClient">The generated client interface/type for the downstream service (an <see cref="IHasHealthCheck"/>), e.g. <c>IOrderServiceClient</c>.</typeparam>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="serviceName">The downstream service's name, used as the check's identifier and dependency name.</param>
    public static IHealthCheckBuilder AddContractCheck<TClient>(this IHealthCheckBuilder builder, string serviceName)
        where TClient : class, IHasHealthCheck
    {
        return builder.AddHealthCheck(resolver =>
            new ClientHealthCheck(serviceName, resolver.GetService<TClient>()));
    }

    /// <summary>
    /// Registers a <see cref="ClientHealthCheck"/> for a downstream service against an explicit client
    /// instance (rather than resolving one from DI).
    /// </summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="serviceName">The downstream service's name, used as the check's identifier and dependency name.</param>
    /// <param name="client">The generated client for the downstream service.</param>
    public static IHealthCheckBuilder AddContractCheck(this IHealthCheckBuilder builder, string serviceName, IHasHealthCheck client)
    {
        return builder.AddHealthCheck(new ClientHealthCheck(serviceName, client));
    }
}
