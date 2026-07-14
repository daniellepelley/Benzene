using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers.Response;

/// <summary>
/// Handles writing a response body for a specific serialization format (e.g. JSON), given an
/// <see cref="IBodySerializer"/> to produce the body with. Wrapped by <see cref="ResponseHandler{T,TContext}"/>
/// so it can be plugged into the pipeline as an <see cref="Benzene.Abstractions.MessageHandlers.Response.ISyncResponseHandler{TContext}"/>.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public interface ISerializationResponseHandler<TContext> where TContext : class
{
    /// <summary>
    /// Writes the response body (and typically content type) for the given result, unless a body has
    /// already been set.
    /// </summary>
    /// <param name="context">The transport-specific context to write the response to.</param>
    /// <param name="messageHandlerResult">The outcome of routing and invoking the handler.</param>
    /// <param name="bodySerializer">Serializes the result into a response body for this context.</param>
    void HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult, IBodySerializer bodySerializer);
}
