using Benzene.Abstractions;
using Benzene.Abstractions.DI;

namespace Benzene.Clients;

/// <summary>
/// Superseded by <c>Benzene.Resilience.RetryMiddleware&lt;OutboundContext&gt;</c>/
/// <c>.UseRetry(...)</c> on an outbound route pipeline. See
/// <c>work/benzene-clients-redesign-plan.md</c>.
/// </summary>
[Obsolete("Use Benzene.Resilience.RetryMiddleware<OutboundContext>/.UseRetry(...) instead - see work/benzene-clients-redesign-plan.md")]
public class RetryBenzeneMessageClientWrapper : IDependencyWrapper<IBenzeneMessageClient>
{
    private readonly int _numberOfRetries;

    public RetryBenzeneMessageClientWrapper(int numberOfRetries)
    {
        _numberOfRetries = numberOfRetries;
    }

    public IBenzeneMessageClient Wrap(IServiceResolver serviceResolver, IBenzeneMessageClient benzeneMessageClient)
    {
        return new RetryBenzeneMessageClient(benzeneMessageClient, _numberOfRetries);
    }
}
