namespace Benzene.Abstractions.MessageHandlers.Request;

/// <summary>
/// Maps a transport-specific context's raw message body into a strongly-typed request object.
/// Wrapped by <see cref="IDeferredRequestMapper"/> so a router can defer this until it knows the
/// handler's request type, and typically implemented on top of an <c>ISerializer</c> and the
/// context's <c>IMessageBodyGetter{TContext}</c>.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type requests are mapped from.</typeparam>
public interface IRequestMapper<in TContext>
{
    /// <summary>Maps the message body in the given context to the requested type.</summary>
    /// <typeparam name="TRequest">The request type to map the body into.</typeparam>
    /// <param name="context">The transport-specific context for the incoming message.</param>
    /// <returns>The mapped request, or <c>null</c> if the body is missing or could not be mapped.</returns>
    TRequest? GetBody<TRequest>(TContext context) where TRequest : class;
}