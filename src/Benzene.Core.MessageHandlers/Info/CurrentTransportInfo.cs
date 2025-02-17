using Benzene.Abstractions.MessageHandlers.Info;

namespace Benzene.Core.MessageHandlers.Info;

public class CurrentTransportInfo : ICurrentTransport, ISetCurrentTransport
{
    public string Name { get; private set; } = Constants.Missing.Id;
    public void SetTransport(string transport)
    {
        Name = transport;
    }
}