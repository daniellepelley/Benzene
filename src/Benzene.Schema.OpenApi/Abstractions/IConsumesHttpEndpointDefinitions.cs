using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Http.Routing;

namespace Benzene.Schema.OpenApi.Abstractions;

public interface IConsumesHttpEndpointDefinitions<out TBuilder>
{
    public TBuilder AddHttpEndpointDefinitions(IHttpEndpointDefinition[] httpEndpointDefinitions,
        IMessageHandlerDefinition[] messageHandlerDefinitions);
}