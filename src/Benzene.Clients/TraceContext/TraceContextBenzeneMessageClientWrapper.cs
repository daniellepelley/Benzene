using Benzene.Abstractions;
using Benzene.Abstractions.DI;

namespace Benzene.Clients.TraceContext;

/// <summary>
/// Wraps a resolved <see cref="IBenzeneMessageClient"/> in a <see cref="TraceContextBenzeneMessageClient"/>.
///
/// Superseded by <see cref="W3CTraceContextMiddleware"/>/<c>.UseW3CTraceContext()</c> on an
/// outbound route pipeline. See <c>work/benzene-clients-redesign-plan.md</c>.
/// </summary>
[Obsolete("Use W3CTraceContextMiddleware/.UseW3CTraceContext() instead - see work/benzene-clients-redesign-plan.md")]
public class TraceContextBenzeneMessageClientWrapper : IDependencyWrapper<IBenzeneMessageClient>
{
    /// <inheritdoc />
    public IBenzeneMessageClient Wrap(IServiceResolver serviceResolver, IBenzeneMessageClient benzeneMessageClient)
    {
        return new TraceContextBenzeneMessageClient(benzeneMessageClient);
    }
}
