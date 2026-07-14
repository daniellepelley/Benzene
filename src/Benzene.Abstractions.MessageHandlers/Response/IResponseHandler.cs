namespace Benzene.Abstractions.MessageHandlers.Response;

/// <summary>
/// Marker interface for a piece of response processing plugged into an
/// <see cref="IResponseHandlerContainer{TContext}"/> (e.g. writing the response body, setting a
/// status code). Has no members itself; implement either <see cref="ISyncResponseHandler{TContext}"/>
/// or <see cref="IAsyncResponseHandler{TContext}"/> depending on whether the work is synchronous or
/// asynchronous -- the container dispatches to the appropriate one at runtime by type-checking each
/// registered handler.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public interface IResponseHandler<TContext>
{ }
