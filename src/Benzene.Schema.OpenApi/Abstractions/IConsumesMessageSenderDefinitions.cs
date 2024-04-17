using Benzene.Abstractions.MessageHandling;

namespace Benzene.Schema.OpenApi.Abstractions;

public interface IConsumesMessageSenderDefinitions<out TBuilder>
{
    public TBuilder AddMessageSenderDefinitions(IMessageDefinition[] messageDefinitions);
}
