using Benzene.Abstractions.Serialization;

namespace Benzene.Abstractions.MessageHandlers.Response;

/// <summary>
/// Converts a message benzeneResult into a response body using the passed serializer 
/// </summary>
public interface IResponsePayloadMapper<TContext>
{
    string Map(TContext context, IMessageHandlerResult messageHandlerResult, ISerializer serializer);
}