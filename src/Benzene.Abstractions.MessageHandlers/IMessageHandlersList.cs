namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// The registry of handler definitions built up at startup from one or more
/// <see cref="IMessageHandlersFinder"/>s, and typically consumed via an
/// <see cref="IMessageHandlerDefinitionLookUp"/> to resolve a handler for an incoming topic.
/// </summary>
public interface IMessageHandlersList
{
    /// <summary>Registers a handler definition.</summary>
    /// <param name="messageHandlerDefinition">The handler definition to add.</param>
    void Add(IMessageHandlerDefinition messageHandlerDefinition);
}