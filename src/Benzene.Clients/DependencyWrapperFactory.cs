using Benzene.Abstractions;
using Benzene.Abstractions.DI;

namespace Benzene.Clients;

/// <summary>
/// Superseded by ordinary outbound middleware (e.g. a future <c>RetryMiddleware&lt;OutboundContext&gt;</c>
/// added via <c>.UseRetry(n)</c> on an <see cref="OutboundRoutingBuilder.Route"/> pipeline) - the
/// framework's own middleware pipeline replaces this parallel decorator-chain mechanism. See
/// <c>work/benzene-clients-redesign-plan.md</c> §2.4.
/// </summary>
[Obsolete("Use outbound pipeline middleware (OutboundRoutingBuilder.Route) instead - see work/benzene-clients-redesign-plan.md")]
public class DependencyWrapperFactory<T>
{
    private readonly IEnumerable<IDependencyWrapper<T>> _dependencyWrappers;

    public DependencyWrapperFactory(IEnumerable<IDependencyWrapper<T>> dependencyWrappers)
    {
        _dependencyWrappers = dependencyWrappers;
    }

    public T Create(IServiceResolver serviceResolver, T source)
    {
        return _dependencyWrappers.Aggregate(source, (m, wrapper) => wrapper.Wrap(serviceResolver, m));
    }
}
