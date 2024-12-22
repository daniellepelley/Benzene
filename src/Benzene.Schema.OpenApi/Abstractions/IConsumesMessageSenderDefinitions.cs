using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Schema.OpenApi.Abstractions;

public interface IConsumesMessageSenderDefinitions<out TBuilder>
{
    public TBuilder AddMessageSenderDefinitions(IMessageDefinition[] messageDefinitions);
}
