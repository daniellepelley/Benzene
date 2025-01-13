using Benzene.Abstractions.Info;

namespace Benzene.Core.Info;

public class TransportInfo : ITransportInfo
{
    public TransportInfo(string name)
    {
        Name = name;
    }

    public string Name { get; } 
}

