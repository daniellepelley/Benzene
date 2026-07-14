using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// The interface application code implements to handle a message and produce a typed response.
/// Equivalent to <see cref="IMessageHandlerBase{TRequest, TResponse}"/>, kept as its own interface
/// (rather than a type alias) so handler discovery (<see cref="IMessageHandlersFinder"/>) and DI
/// registration can consistently look for <c>IMessageHandler&lt;,&gt;</c> as the "this is a handler"
/// marker, while <see cref="IMessageHandlerBase{TRequest, TResponse}"/> stays available as the
/// narrower contract other abstractions (e.g. wrapping/decoration) can depend on without pulling in
/// handler-discovery semantics.
/// </summary>
/// <typeparam name="TRequest">The strongly-typed request this handler accepts.</typeparam>
/// <typeparam name="TResponse">The strongly-typed response this handler returns.</typeparam>
public interface IMessageHandler<TRequest, TResponse>
    : IMessageHandlerBase<TRequest, TResponse>
{}

/// <summary>
/// The interface application code implements to handle a message that produces no response payload
/// (fire-and-forget style, e.g. an event handler). Handler discovery wraps implementations of this
/// interface so they can still flow through the same <see cref="IMessageHandler{TRequest, TResponse}"/>
/// based pipeline as request/response handlers (see <c>IMessageHandlerWrapper</c>).
/// </summary>
/// <typeparam name="TRequest">The strongly-typed request this handler accepts.</typeparam>
public interface IMessageHandler<TRequest>
{
    /// <summary>Handles the given request. No response payload is produced.</summary>
    /// <param name="request">The strongly-typed request to handle.</param>
    Task HandleAsync(TRequest request);
}

/// <summary>
/// The non-generic, transport-facing entry point for invoking a resolved message handler. This is
/// what <see cref="IMessageHandlerFactory"/> returns and what a router/dispatcher (e.g.
/// <c>MessageRouter&lt;TContext&gt;</c>) calls: it hides the handler's concrete request/response types
/// behind <see cref="IRequestMapperThunk"/>, since the router only knows the topic being handled, not
/// the handler's generic type arguments, until it resolves the handler.
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Maps the incoming message to the handler's request type via <paramref name="requestMapperThunk"/>
    /// and invokes the handler, returning its result as an untyped <see cref="IBenzeneResult"/>.
    /// </summary>
    /// <param name="requestMapperThunk">
    /// Deferred request mapper that can produce the handler's specific request type on demand.
    /// </param>
    /// <returns>The outcome of handling the message.</returns>
    Task<IBenzeneResult> HandlerAsync(IRequestMapperThunk requestMapperThunk);
}