using Benzene.Abstractions.Messages;

namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// Resolves the <see cref="IMessageHandlerDefinition"/> registered for a given topic, so a router
/// (e.g. <c>MessageRouter&lt;TContext&gt;</c>) can find which handler to invoke for an incoming
/// message. Distinct from <see cref="IMessageHandlersFinder"/>: a finder discovers definitions
/// (e.g. via reflection), while this interface is the lookup surface used at request-handling time,
/// typically backed by a cached/indexed view of definitions discovered up front.
/// </summary>
public interface IMessageHandlerDefinitionLookUp
{
    /// <summary>Finds the handler definition registered for the given topic.</summary>
    /// <param name="topic">The topic (id + version) to find a handler for.</param>
    /// <returns>The matching handler definition, or <c>null</c> if no handler is registered for the topic.</returns>
    IMessageHandlerDefinition? FindHandler(ITopic topic);

    /// <summary>Returns every handler definition currently registered.</summary>
    /// <returns>All registered handler definitions.</returns>
    IMessageHandlerDefinition[] GetAllHandlers();
}