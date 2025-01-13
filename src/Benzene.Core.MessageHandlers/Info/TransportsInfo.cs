using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Info;

namespace Benzene.Core.Info;

public class TransportsInfo : ITransportsInfo
{
    public TransportsInfo(IEnumerable<ITransportInfo> transportInfos)
    {
        Transports = transportInfos.Select(x => x.Name).Distinct().ToArray();
    }
    
    public string[] Transports { get; }
}
