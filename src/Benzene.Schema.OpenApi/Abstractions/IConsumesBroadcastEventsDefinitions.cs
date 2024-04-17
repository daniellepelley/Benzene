using Benzene.Abstractions.MessageHandling;

namespace Benzene.Schema.OpenApi.Abstractions;

public interface IConsumesBroadcastEventsDefinitions<out TBuilder>
{
    public TBuilder AddBroadcastEventDefinitions(IMessageDefinition[] messageDefinitions);
}
