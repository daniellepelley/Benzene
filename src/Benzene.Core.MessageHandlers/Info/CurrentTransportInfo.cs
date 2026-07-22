using Benzene.Abstractions.MessageHandlers.Info;

namespace Benzene.Core.MessageHandlers.Info;

/// <summary>
/// Default <see cref="ICurrentTransport"/>/<see cref="ISetCurrentTransport"/> implementation,
/// registered scoped so each invocation gets its own current-transport value. Starts out set to
/// <see cref="TransportNames.Unresolved"/> until a transport pipeline (e.g. via
/// <see cref="TransportMiddlewarePipeline{TContext}"/>) records itself.
/// </summary>
public class CurrentTransportInfo : ICurrentTransport, ISetCurrentTransport
{
    /// <inheritdoc cref="ICurrentTransport.Name" />
    public string Name { get; private set; } = TransportNames.Unresolved;

    /// <inheritdoc />
    public void SetTransport(string transport)
    {
        Name = transport;
    }
}
