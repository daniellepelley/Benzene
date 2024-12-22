using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Results;

namespace Benzene.Abstractions.Response;

/// <summary>
/// Converts a message result into a response body using the passed serializer 
/// </summary>
public interface IResponsePayloadMapper<TContext>
{
    string Map(TContext context, IMessageHandlerResult messageHandlerResult, ISerializer serializer);
}