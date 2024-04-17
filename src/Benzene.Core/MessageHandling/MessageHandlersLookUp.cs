using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.MessageHandling;

namespace Benzene.Core.MessageHandling;

public class MessageHandlersLookUp : IMessageHandlersLookUp
{
    private readonly IEnumerable<IMessageHandlersFinder> _messageHandlersFinder;
    private readonly IVersionSelector _versionSelector;

    public MessageHandlersLookUp(IEnumerable<IMessageHandlersFinder> messageHandlersFinder, IVersionSelector versionSelector)
    {
        _versionSelector = versionSelector;
        _messageHandlersFinder = messageHandlersFinder;
    }

    public IMessageHandlerDefinition FindHandler(ITopic topic)
    {
        var handlers = GetMessageHandlers()
            .Where(x => x.Topic == topic.Id)
            .ToArray();

        return handlers.FirstOrDefault(x =>
            x.Version == _versionSelector.Select(topic.Version, handlers.Select(x1 => x1.Version).ToArray()));
    }

    public IMessageHandlerDefinition[] GetAllHandlers()
    {
        return GetMessageHandlers();
    }

    private IMessageHandlerDefinition[] GetMessageHandlers()
    {
        return _messageHandlersFinder
            .SelectMany(x => x.FindDefinitions())
            .GroupBy(x => new {x.Topic, x.Version})
            .Select(x => x.First())
            .ToArray();
    }
}