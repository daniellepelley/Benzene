using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IMessageHandlerDefinitionLookUp"/> implementation that aggregates every
/// registered <see cref="IMessageHandlersFinder"/>, de-duplicates definitions by topic id/version,
/// and resolves the requested version via <see cref="IVersionSelector"/>.
/// </summary>
public class MessageHandlerDefinitionLookUp : IMessageHandlerDefinitionLookUp
{
    private readonly IEnumerable<IMessageHandlersFinder> _messageHandlersFinder;
    private readonly IVersionSelector _versionSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHandlerDefinitionLookUp"/> class.
    /// </summary>
    /// <param name="messageHandlersFinder">Every registered handler finder to aggregate definitions from.</param>
    /// <param name="versionSelector">Resolves which version of a handler to use when several are registered for the same topic.</param>
    public MessageHandlerDefinitionLookUp(IEnumerable<IMessageHandlersFinder> messageHandlersFinder, IVersionSelector versionSelector)
    {
        _versionSelector = versionSelector;
        _messageHandlersFinder = messageHandlersFinder;
    }

    /// <summary>
    /// Finds the handler definition registered for the given topic, using <see cref="IVersionSelector"/>
    /// to pick between definitions when more than one version is registered for the same topic id.
    /// </summary>
    /// <param name="topic">The topic (id + requested version) to find a handler for.</param>
    /// <returns>The matching handler definition, or <c>null</c>/default if none is registered for the topic id.</returns>
    public IMessageHandlerDefinition FindHandler(ITopic topic)
    {
        var handlers = GetMessageHandlers()
            .Where(x => x.Topic.Id == topic.Id)
            .ToArray();

        return handlers.FirstOrDefault(x =>
            x.Topic.Version == _versionSelector.Select(topic.Version, handlers
                .Select(x1 => x1.Topic.Version).ToArray()));
    }

    /// <summary>
    /// Returns every distinct handler definition (by topic id + version) across all registered finders.
    /// </summary>
    /// <returns>All registered handler definitions.</returns>
    public IMessageHandlerDefinition[] GetAllHandlers()
    {
        return GetMessageHandlers();
    }

    private IMessageHandlerDefinition[] GetMessageHandlers()
    {
        return _messageHandlersFinder
            .SelectMany(x => x.FindDefinitions())
            .GroupBy(x => new {x.Topic.Id, x.Topic.Version})
            .Select(x => x.First())
            .ToArray();
    }
}
