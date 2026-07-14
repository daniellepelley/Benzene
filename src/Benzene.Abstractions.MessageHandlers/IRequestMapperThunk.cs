namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// A deferred, request-type-agnostic handle onto the current message's <see cref="Request.IRequestMapper{TContext}"/>
/// and transport context, closed over by the router before the handler's concrete request type is
/// known. This lets the non-generic <see cref="IMessageHandler"/> ask for the request in whatever
/// type the handler actually needs (<c>GetRequest&lt;TRequest&gt;</c>) without the router itself
/// needing to be generic over that type.
/// </summary>
public interface IRequestMapperThunk
{
    /// <summary>Maps the current message to the given request type.</summary>
    /// <typeparam name="TRequest">The request type expected by the handler.</typeparam>
    /// <returns>The mapped request, or <c>null</c> if it could not be mapped.</returns>
    TRequest? GetRequest<TRequest>() where TRequest : class;
}