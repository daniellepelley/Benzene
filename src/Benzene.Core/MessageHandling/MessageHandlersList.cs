using System.Collections.Generic;
using Benzene.Abstractions.MessageHandling;

namespace Benzene.Core.MessageHandling;


public class MessageHandlersList : IMessageHandlersFinder, IMessageHandlersList
{
    private readonly List<IMessageHandlerDefinition> _list = new();

    public IMessageHandlerDefinition[] FindDefinitions()
    {
        return _list.ToArray();
    }

    public void Add(IMessageHandlerDefinition messageHandlerDefinition)
    {
        _list.Add(messageHandlerDefinition);
    }
}
