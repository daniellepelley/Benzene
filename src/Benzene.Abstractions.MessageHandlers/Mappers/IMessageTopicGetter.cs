using Benzene.Abstractions.Messages;

namespace Benzene.Abstractions.MessageHandlers.Mappers;

/// <summary>
/// Extracts the routing topic from a transport-specific message context, so a router can look up the
/// registered handler via <see cref="IMessageHandlerDefinitionLookUp"/>.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the topic is extracted from.</typeparam>
public interface IMessageTopicGetter<TContext>
{
    /// <summary>Extracts the topic from the given context.</summary>
    /// <param name="context">The transport-specific context for the incoming message.</param>
    /// <returns>The topic to route the message on, or <c>null</c> if it could not be determined.</returns>
    ITopic? GetTopic(TContext context);
}