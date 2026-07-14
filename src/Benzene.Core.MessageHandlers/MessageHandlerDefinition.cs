using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Immutable metadata describing one discovered message handler: the topic (and version) it
/// answers, its request/response types, and the CLR type implementing it. Produced by
/// <see cref="IMessageHandlersFinder"/> implementations and consumed by
/// <see cref="IMessageHandlerDefinitionLookUp"/> and <see cref="MessageHandlerFactory"/>.
/// </summary>
public class MessageHandlerDefinition : IMessageHandlerDefinition
{
    private MessageHandlerDefinition(ITopic topic, Type requestType, Type responseType, Type handlerType)
    {
        Topic = topic;
        RequestType = requestType;
        ResponseType = responseType;
        HandlerType = handlerType;
    }

    /// <summary>
    /// Creates a definition for a handler that has a response type and an explicit version.
    /// </summary>
    /// <param name="topic">The topic id the handler answers.</param>
    /// <param name="version">The version of the handler.</param>
    /// <param name="requestType">The handler's request type.</param>
    /// <param name="responseType">The handler's response type.</param>
    /// <param name="handlerType">The CLR type implementing the handler.</param>
    /// <returns>A new <see cref="MessageHandlerDefinition"/>.</returns>
    public static MessageHandlerDefinition CreateInstance(string topic, string version, Type requestType, Type responseType, Type handlerType)
    {
        return new MessageHandlerDefinition(new Topic(topic, version), requestType, responseType, handlerType);
    }

    /// <summary>
    /// Creates a definition for a handler that has a response type, without a specific version.
    /// </summary>
    /// <param name="topic">The topic id the handler answers.</param>
    /// <param name="requestType">The handler's request type.</param>
    /// <param name="responseType">The handler's response type.</param>
    /// <param name="handlerType">The CLR type implementing the handler.</param>
    /// <returns>A new <see cref="MessageHandlerDefinition"/>.</returns>
    public static MessageHandlerDefinition CreateInstance(string topic, Type requestType, Type responseType, Type handlerType)
    {
        return new MessageHandlerDefinition(new Topic(topic), requestType, responseType, handlerType);
    }

    /// <summary>
    /// Creates a definition without a handler type, using <see cref="Void"/> as the response type.
    /// Used where only the request/response shape is known ahead of a concrete handler being resolved.
    /// </summary>
    /// <param name="topic">The topic id the handler answers.</param>
    /// <param name="requestType">The handler's request type.</param>
    /// <param name="responseType">The handler's response type.</param>
    /// <returns>A new <see cref="MessageHandlerDefinition"/> with <see cref="HandlerType"/> set to <see cref="Void"/>.</returns>
    public static MessageHandlerDefinition CreateInstance(string topic, Type requestType, Type responseType)
    {
        return new MessageHandlerDefinition(new Topic(topic), requestType, responseType, typeof(Void));
    }

    /// <summary>
    /// Creates the sentinel "no handler found" definition, using <see cref="Constants.Missing"/> as
    /// the topic and <see cref="Void"/> for every type. Used by <see cref="MessageRouter{TContext}"/>
    /// to report a missing/unresolvable handler via the normal <see cref="IMessageHandlerResult"/> shape.
    /// </summary>
    /// <returns>The empty/sentinel <see cref="MessageHandlerDefinition"/>.</returns>
    public static MessageHandlerDefinition Empty()
    {
        return new MessageHandlerDefinition(new Topic(Constants.Missing.Id), typeof(Void), typeof(Void), typeof(Void));
    }

    /// <inheritdoc />
    public ITopic Topic { get; init; }

    /// <inheritdoc />
    public Type RequestType { get; init; }

    /// <inheritdoc />
    public Type ResponseType { get; init; }

    /// <inheritdoc />
    public Type HandlerType { get; init; }

}
