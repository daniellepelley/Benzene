using Benzene.Abstractions.MessageHandlers.Info;

namespace Benzene.Core.MessageHandlers.Info;

/// <summary>
/// Default <see cref="ITransportsInfo"/> implementation, aggregating the distinct names of every
/// <see cref="ITransportInfo"/> registered in DI (one per transport adapter in use).
/// </summary>
public class TransportsInfo : ITransportsInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransportsInfo"/> class.
    /// </summary>
    /// <param name="transportInfos">Every registered transport's info.</param>
    public TransportsInfo(IEnumerable<ITransportInfo> transportInfos)
    {
        Transports = transportInfos.Select(x => x.Name).Distinct().ToArray();
    }

    /// <inheritdoc />
    public string[] Transports { get; }
}
