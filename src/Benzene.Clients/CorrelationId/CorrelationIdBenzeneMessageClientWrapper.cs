using Benzene.Abstractions;
using Benzene.Abstractions.DI;

namespace Benzene.Clients.CorrelationId;

/// <summary>
/// Superseded by <see cref="CorrelationIdMiddleware"/>/<c>.UseCorrelationId()</c> on an outbound
/// route pipeline. See <c>work/benzene-clients-redesign-plan.md</c>.
/// </summary>
[Obsolete("Use CorrelationIdMiddleware/.UseCorrelationId() instead - see work/benzene-clients-redesign-plan.md")]
public class CorrelationIdBenzeneMessageClientWrapper : IDependencyWrapper<IBenzeneMessageClient>
{
    public IBenzeneMessageClient Wrap(IServiceResolver serviceResolver, IBenzeneMessageClient benzeneMessageClient)
    {
        return new CorrelationIdBenzeneMessageClient(benzeneMessageClient, serviceResolver.Resolve<ICorrelationId>());
    }
}
