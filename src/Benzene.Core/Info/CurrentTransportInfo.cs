using Benzene.Abstractions.Info;

namespace Benzene.Core.Info;

public class CurrentTransportInfo : ICurrentTransport, ISetCurrentTransport
{
    public string Name { get; private set; } = Constants.Missing;
    public void SetTransport(string transport)
    {
        Name = transport;
    }
}

