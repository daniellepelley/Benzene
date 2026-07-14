using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IMessageHandlerDefinitionLookUp"/> implementation that resolves the requested
/// version via <see cref="IVersionSelector"/> against the shared <see cref="MessageHandlerDefinitionIndex"/>.
/// </summary>
/// <remarks>
/// This type is constructed fresh per message (scoped DI lifetime), matching every other per-message
/// pipeline collaborator; the aggregation and de-duplication over every registered
/// <see cref="IMessageHandlersFinder"/> is done once, in the singleton <see cref="MessageHandlerDefinitionIndex"/>,
/// not repeated per instance.
/// </remarks>
public class MessageHandlerDefinitionLookUp : IMessageHandlerDefinitionLookUp
{
    private readonly MessageHandlerDefinitionIndex _index;
    private readonly IVersionSelector _versionSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHandlerDefinitionLookUp"/> class.
    /// </summary>
    /// <param name="index">The shared index over every registered handler finder's definitions.</param>
    /// <param name="versionSelector">Resolves which version of a handler to use when several are registered for the same topic.</param>
    public MessageHandlerDefinitionLookUp(MessageHandlerDefinitionIndex index, IVersionSelector versionSelector)
    {
        _index = index;
        _versionSelector = versionSelector;
    }

    /// <summary>
    /// Finds the handler definition registered for the given topic, using <see cref="IVersionSelector"/>
    /// to pick between definitions when more than one version is registered for the same topic id.
    /// </summary>
    /// <param name="topic">The topic (id + requested version) to find a handler for.</param>
    /// <returns>The matching handler definition, or <c>null</c>/default if none is registered for the topic id.</returns>
    public IMessageHandlerDefinition FindHandler(ITopic topic)
    {
        var handlers = _index.GetByTopicId(topic.Id);
        if (handlers.Length == 0)
        {
            return null;
        }

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
        return _index.GetAll();
    }
}
