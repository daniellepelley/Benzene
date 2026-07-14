using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// Routing metadata shared by both the generic and non-generic outcome of dispatching a message to
/// a handler, independent of whether a response payload was produced.
/// </summary>
public interface IMessageHandlerResultBase
{
    /// <summary>
    /// The topic the message was routed on, or <c>null</c> if the topic itself could not be
    /// determined (e.g. it was missing from the incoming message).
    /// </summary>
    ITopic? Topic { get; }

    /// <summary>
    /// The definition of the handler that was invoked, or <c>null</c> if no handler was found for
    /// the topic (in which case <see cref="IMessageHandlerResult.BenzeneResult"/> will typically
    /// carry a not-found style outcome).
    /// </summary>
    IMessageHandlerDefinition? MessageHandlerDefinition { get; }
}

/// <summary>
/// The outcome of routing and invoking a handler for one message, produced by a router (e.g.
/// <c>MessageRouter&lt;TContext&gt;</c>) and passed to an <c>IMessageHandlerResultSetter{TContext}</c>
/// to be written back to the transport.
/// </summary>
public interface IMessageHandlerResult : IMessageHandlerResultBase
{
    /// <summary>The untyped result returned by the handler (or a routing failure, e.g. not-found).</summary>
    IBenzeneResult BenzeneResult { get; }
}

/// <summary>
/// Strongly-typed variant of <see cref="IMessageHandlerResult"/> carrying the handler's response
/// payload type, used where the caller already knows the expected response type at compile time.
/// </summary>
/// <typeparam name="TResponse">The strongly-typed response payload produced by the handler.</typeparam>
public interface IMessageHandlerResult<TResponse> : IMessageHandlerResultBase
{
    /// <summary>The typed result returned by the handler (or a routing failure, e.g. not-found).</summary>
    IBenzeneResult<TResponse> BenzeneResult { get; }
}