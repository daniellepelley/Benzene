using Benzene.Abstractions.MessageHandlers.Info;

namespace Benzene.Core.MessageHandlers.Info;

public class TransportInfo : ITransportInfo
{
    public TransportInfo(string name)
    {
        Name = name;
    }

    public string Name { get; } 
}

