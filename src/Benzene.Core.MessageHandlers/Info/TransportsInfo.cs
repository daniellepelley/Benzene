using Benzene.Abstractions.Info;

namespace Benzene.Core.MessageHandlers.Info;

public class TransportsInfo : ITransportsInfo
{
    public TransportsInfo(IEnumerable<ITransportInfo> transportInfos)
    {
        Transports = transportInfos.Select(x => x.Name).Distinct().ToArray();
    }
    
    public string[] Transports { get; }
}
