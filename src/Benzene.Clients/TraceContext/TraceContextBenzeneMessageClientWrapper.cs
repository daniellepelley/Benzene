using Benzene.Abstractions;
using Benzene.Abstractions.DI;

namespace Benzene.Clients.TraceContext;

/// <summary>
/// Wraps a resolved <see cref="IBenzeneMessageClient"/> in a <see cref="TraceContextBenzeneMessageClient"/>.
/// </summary>
public class TraceContextBenzeneMessageClientWrapper : IDependencyWrapper<IBenzeneMessageClient>
{
    /// <inheritdoc />
    public IBenzeneMessageClient Wrap(IServiceResolver serviceResolver, IBenzeneMessageClient benzeneMessageClient)
    {
        return new TraceContextBenzeneMessageClient(benzeneMessageClient);
    }
}
