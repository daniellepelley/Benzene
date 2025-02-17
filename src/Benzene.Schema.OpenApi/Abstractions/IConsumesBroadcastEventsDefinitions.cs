using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;

namespace Benzene.Schema.OpenApi.Abstractions;

public interface IConsumesBroadcastEventsDefinitions<out TBuilder>
{
    public TBuilder AddBroadcastEventDefinitions(IMessageDefinition[] messageDefinitions);
}
