using Benzene.Abstractions.MessageHandlers.Info;

namespace Benzene.Core.MessageHandlers.Info;

/// <summary>
/// Default <see cref="ITransportInfo"/> implementation. Typically registered once per transport
/// adapter (e.g. <c>new TransportInfo("direct")</c> for the <c>BenzeneMessage</c> transport), and
/// aggregated by <see cref="TransportsInfo"/>.
/// </summary>
public class TransportInfo : ITransportInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransportInfo"/> class.
    /// </summary>
    /// <param name="name">The transport's name.</param>
    public TransportInfo(string name)
    {
        Name = name;
    }

    /// <inheritdoc />
    public string Name { get; }
}

