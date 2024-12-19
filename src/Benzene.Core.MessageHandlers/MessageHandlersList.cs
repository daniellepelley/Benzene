﻿using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;

namespace Benzene.Core.MessageHandlers;


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
