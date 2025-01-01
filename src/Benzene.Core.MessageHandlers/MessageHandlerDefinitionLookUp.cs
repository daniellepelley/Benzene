using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;

namespace Benzene.Core.MessageHandlers;

public class MessageHandlerDefinitionLookUp : IMessageHandlerDefinitionLookUp
{
    private readonly IEnumerable<IMessageHandlersFinder> _messageHandlersFinder;
    private readonly IVersionSelector _versionSelector;

    public MessageHandlerDefinitionLookUp(IEnumerable<IMessageHandlersFinder> messageHandlersFinder, IVersionSelector versionSelector)
    {
        _versionSelector = versionSelector;
        _messageHandlersFinder = messageHandlersFinder;
    }

    public IMessageHandlerDefinition FindHandler(ITopic topic)
    {
        var handlers = GetMessageHandlers()
            .Where(x => x.Topic.Id == topic.Id)
            .ToArray();

        return handlers.FirstOrDefault(x =>
            x.Topic.Version == _versionSelector.Select(topic.Version, handlers
                .Select(x1 => x1.Topic.Version).ToArray()));
    }

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