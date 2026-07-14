namespace Benzene.Abstractions.MessageHandlers.Request;

/// <summary>
/// Minimal read-only view onto the already-mapped, strongly-typed request for a context type, used
/// where code only needs to read the request (e.g. a request enricher or filter) without depending
/// on the full <see cref="IMessageHandlerContext{TRequest, TResponse}"/> contract.
/// </summary>
/// <typeparam name="TRequest">The strongly-typed request exposed by this context.</typeparam>
public interface IRequestContext<TRequest>
{
    /// <summary>The strongly-typed request for the current invocation.</summary>
    TRequest Request { get; }
}