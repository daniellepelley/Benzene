namespace Benzene.Abstractions.MessageHandlers.Response;

/// <summary>
/// One piece of response processing plugged into an <see cref="IResponseHandlerContainer{TContext}"/>
/// (e.g. writing the response body, setting a status code), run in registration order against a
/// handler's result. A single interface for both synchronous work (e.g. setting a status code -
/// return <c>default</c>) and asynchronous work (e.g. writing to a network stream), replacing the
/// previous <c>ISyncResponseHandler</c>/<c>IAsyncResponseHandler</c> split - that split required
/// <see cref="IResponseHandlerContainer{TContext}"/> to type-switch each registered handler at
/// runtime and silently skip anything implementing neither.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public interface IResponseHandler<TContext>
{
    /// <summary>Processes the handler's result against the transport context.</summary>
    /// <param name="context">The transport-specific context to write the response to.</param>
    /// <param name="messageHandlerResult">The outcome of routing and invoking the handler.</param>
    ValueTask HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}
