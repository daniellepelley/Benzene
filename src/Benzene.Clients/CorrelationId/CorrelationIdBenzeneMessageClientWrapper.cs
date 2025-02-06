using Benzene.Abstractions;
using Benzene.Abstractions.DI;

namespace Benzene.Clients.CorrelationId;

public class CorrelationIdBenzeneMessageClientWrapper : IDependencyWrapper<IBenzeneMessageClient>
{
    public IBenzeneMessageClient Wrap(IServiceResolver serviceResolver, IBenzeneMessageClient benzeneMessageClient)
    {
        return new CorrelationIdBenzeneMessageClient(benzeneMessageClient, serviceResolver.Resolve<ICorrelationId>());
    }
}
