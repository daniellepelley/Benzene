using Benzene.Abstractions.MessageHandling;

namespace Benzene.Schema.OpenApi.Abstractions;

public interface IConsumesMessageHandlerDefinitions<out TBuilder>
{
    public TBuilder AddMessageHandlerDefinitions(IMessageHandlerDefinition[] messageHandlerDefinitions);
}
