using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;

namespace Benzene.Core.MessageHandling;

public class DependencyMessageHandlersFinder : IMessageHandlersFinder
{
    private readonly IEnumerable<IMessageHandlerDefinition> _messageHandlerDefinitions;

    public DependencyMessageHandlersFinder(IEnumerable<IMessageHandlerDefinition> messageHandlerDefinitions)
    {
        _messageHandlerDefinitions = messageHandlerDefinitions;
    }
    public IMessageHandlerDefinition[] FindDefinitions()
    {
        return _messageHandlerDefinitions.ToArray();
    }
}