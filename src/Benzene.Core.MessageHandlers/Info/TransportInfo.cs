using Benzene.Abstractions.Info;

namespace Benzene.Core.MessageHandlers.Info;

public class TransportInfo : ITransportInfo
{
    public TransportInfo(string name)
    {
        Name = name;
    }

    public string Name { get; } 
}

