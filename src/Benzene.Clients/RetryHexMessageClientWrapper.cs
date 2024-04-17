using Benzene.Abstractions;
using Benzene.Abstractions.DI;

namespace Benzene.Clients;

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
