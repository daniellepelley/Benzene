using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IMessageHandlerResult"/> implementation, produced by <see cref="MessageRouter{TContext}"/>
/// after routing and (attempting to) invoke a handler for one message.
/// </summary>
public class MessageHandlerResult : IMessageHandlerResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHandlerResult"/> class with full routing metadata.
    /// </summary>
    /// <param name="topic">The topic the message was routed on, or <c>null</c> if it could not be determined.</param>
    /// <param name="messageHandlerDefinition">The definition of the handler that was invoked, or <c>null</c> if none was found.</param>
    /// <param name="benzeneResult">The untyped outcome of handling (or attempting to route) the message.</param>
    public MessageHandlerResult(ITopic? topic, IMessageHandlerDefinition? messageHandlerDefinition, IBenzeneResult benzeneResult)
    {
        Topic = topic;
        MessageHandlerDefinition = messageHandlerDefinition;
        BenzeneResult = benzeneResult;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHandlerResult"/> class with no routing metadata
    /// (<see cref="Topic"/> and <see cref="MessageHandlerDefinition"/> left <c>null</c>).
    /// </summary>
    /// <param name="benzeneResult">The untyped outcome of handling the message.</param>
    public MessageHandlerResult(IBenzeneResult benzeneResult)
    {
        BenzeneResult = benzeneResult;
    }

    /// <inheritdoc />
    public ITopic? Topic { get; }

    /// <inheritdoc />
    public IMessageHandlerDefinition? MessageHandlerDefinition { get; }

    /// <inheritdoc />
    public IBenzeneResult BenzeneResult { get; }
}

/// <summary>
/// Strongly-typed variant of <see cref="MessageHandlerResult"/>, carrying the handler's typed response payload.
/// </summary>
/// <typeparam name="TResponse">The strongly-typed response payload produced by the handler.</typeparam>
public class MessageHandlerResult<TResponse> : IMessageHandlerResult<TResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHandlerResult{TResponse}"/> class with full routing metadata.
    /// </summary>
    /// <param name="topic">The topic the message was routed on, or <c>null</c> if it could not be determined.</param>
    /// <param name="messageHandlerDefinition">The definition of the handler that was invoked, or <c>null</c> if none was found.</param>
    /// <param name="benzeneResult">The typed outcome of handling the message.</param>
    public MessageHandlerResult(ITopic? topic, IMessageHandlerDefinition? messageHandlerDefinition, IBenzeneResult<TResponse> benzeneResult)
    {
        Topic = topic;
        MessageHandlerDefinition = messageHandlerDefinition;
        BenzeneResult = benzeneResult;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHandlerResult{TResponse}"/> class with no routing metadata
    /// (<see cref="Topic"/> and <see cref="MessageHandlerDefinition"/> left <c>null</c>).
    /// </summary>
    /// <param name="benzeneResult">The typed outcome of handling the message.</param>
    public MessageHandlerResult(IBenzeneResult<TResponse> benzeneResult)
    {
        BenzeneResult = benzeneResult;
    }

    /// <inheritdoc />
    public ITopic? Topic { get; }

    /// <inheritdoc />
    public IMessageHandlerDefinition? MessageHandlerDefinition { get; }

    /// <inheritdoc />
    public IBenzeneResult<TResponse> BenzeneResult { get; }

    /// <summary>
    /// Converts a strongly-typed <see cref="MessageHandlerResult{TResponse}"/> to the untyped
    /// <see cref="MessageHandlerResult"/>, carrying the same routing metadata and the underlying
    /// untyped <see cref="IBenzeneResult"/>.
    /// </summary>
    /// <param name="messageHandlerResult">The typed result to convert.</param>
    public static explicit operator MessageHandlerResult(MessageHandlerResult<TResponse> messageHandlerResult)
        => new MessageHandlerResult(messageHandlerResult.Topic, messageHandlerResult.MessageHandlerDefinition, messageHandlerResult.BenzeneResult);

}
