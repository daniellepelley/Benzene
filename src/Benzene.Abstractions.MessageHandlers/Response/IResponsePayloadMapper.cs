using Benzene.Abstractions.Serialization;

namespace Benzene.Abstractions.MessageHandlers.Response;

/// <summary>
/// Converts a message handler's result into a serialized response body using the given serializer.
/// Used by response handlers such as <c>SerializationResponseHandler{TContext}</c> so the actual
/// serialized payload format stays independent of how the body is written onto the transport context.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the result was produced for.</typeparam>
public interface IResponsePayloadMapper<TContext>
{
    /// <summary>Maps a handler's result into a serialized response body.</summary>
    /// <param name="context">The transport-specific context for the current invocation.</param>
    /// <param name="messageHandlerResult">The outcome of routing and invoking the handler.</param>
    /// <param name="serializer">The serializer to use to produce the response body.</param>
    /// <returns>The serialized response body.</returns>
    string Map(TContext context, IMessageHandlerResult messageHandlerResult, ISerializer serializer);
}