using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers.Response;

/// <summary>
/// Default <see cref="IResponsePayloadMapper{TContext}"/> implementation: serializes the handler's
/// success payload using its declared response type, or an <see cref="ErrorPayload"/> built from the
/// result's status and errors on failure.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the result was produced for.</typeparam>
public class DefaultResponsePayloadMapper<TContext> : IResponsePayloadMapper<TContext>
{
    /// <summary>
    /// Maps a handler's result into a serialized response body: the success payload if
    /// <see cref="IBenzeneResult.IsSuccessful"/>, otherwise a serialized <see cref="ErrorPayload"/>.
    /// </summary>
    /// <param name="context">Unused; the mapping does not depend on the transport context.</param>
    /// <param name="messageHandlerResult">The outcome of routing and invoking the handler.</param>
    /// <param name="serializer">The serializer to use to produce the response body.</param>
    /// <returns>
    /// The serialized response body, or <c>null</c> if <see cref="IMessageHandlerResultBase.MessageHandlerDefinition"/>
    /// is <c>null</c> (no handler was resolved) or the success payload itself is <c>null</c>.
    /// </returns>
    public string Map(TContext context, IMessageHandlerResult messageHandlerResult, ISerializer serializer)
    {
        if (messageHandlerResult.MessageHandlerDefinition == null)
        {
            return null;
        }

        return messageHandlerResult.BenzeneResult.IsSuccessful
            ? SerializePayload(messageHandlerResult.MessageHandlerDefinition.ResponseType, messageHandlerResult.BenzeneResult.PayloadAsObject, serializer)
            : serializer.Serialize(AsErrorPayload(messageHandlerResult.BenzeneResult));
    }

    private static ErrorPayload AsErrorPayload(IBenzeneResult benzeneResult)
    {
        return new ErrorPayload(benzeneResult.Status, benzeneResult.Errors);
    }

    private string SerializePayload(Type type, object payload, ISerializer serializer)
    {
        if (payload == null)
        {
            return null;
        }

        if (payload is IRawStringMessage rawStringMessage)
        {
            return rawStringMessage.Content;
        }

        return serializer.Serialize(type, payload);
    }
}
