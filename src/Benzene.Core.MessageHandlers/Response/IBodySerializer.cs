using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.MessageHandlers.Response;

/// <summary>
/// Serializes a handler's result into a response body for a fixed, already-known context, so a
/// consumer such as <see cref="ISerializationResponseHandler{TContext}"/> can produce a body without
/// itself depending on <see cref="Benzene.Abstractions.MessageHandlers.Response.IResponsePayloadMapper{TContext}"/> generically.
/// </summary>
public interface IBodySerializer
{
    /// <summary>
    /// Serializes the given result into a response body using <paramref name="serializer"/>.
    /// </summary>
    /// <param name="serializer">The serializer to use to produce the response body.</param>
    /// <param name="messageHandlerResult">The outcome of routing and invoking the handler.</param>
    /// <returns>The serialized response body.</returns>
    string Serialize(ISerializer serializer, IMessageHandlerResult messageHandlerResult);
}
