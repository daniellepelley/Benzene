using Benzene.Abstractions.MessageHandling;
using Benzene.Http;

namespace Benzene.Schema.OpenApi.Abstractions;

public interface IConsumesHttpEndpointDefinitions<out TBuilder>
{
    public TBuilder AddHttpEndpointDefinitions(IHttpEndpointDefinition[] httpEndpointDefinitions,
        IMessageHandlerDefinition[] messageHandlerDefinitions);
}