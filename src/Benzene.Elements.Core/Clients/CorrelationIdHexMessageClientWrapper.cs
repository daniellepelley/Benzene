using Benzene.Abstractions;
using Benzene.Abstractions.DI;
using Benzene.Clients;
using Benzene.Core.Correlation;

namespace Benzene.Elements.Core.Clients;

public class CorrelationIdBenzeneMessageClientWrapper : IDependencyWrapper<IBenzeneMessageClient>
{
    public IBenzeneMessageClient Wrap(IServiceResolver serviceResolver, IBenzeneMessageClient benzeneMessageClient)
    {
        return new CorrelationIdBenzeneMessageClient(benzeneMessageClient, serviceResolver.Resolve<ICorrelationId>());
    }
}
