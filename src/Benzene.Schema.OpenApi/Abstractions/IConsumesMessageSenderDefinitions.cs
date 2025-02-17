using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;

namespace Benzene.Schema.OpenApi.Abstractions;

public interface IConsumesMessageSenderDefinitions<out TBuilder>
{
    public TBuilder AddMessageSenderDefinitions(IMessageDefinition[] messageDefinitions);
}
