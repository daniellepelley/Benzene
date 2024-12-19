using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;

namespace Benzene.Core.MessageHandlers;

public class CacheMessageHandlersFinder : IMessageHandlersFinder
{
    private readonly IMessageHandlersFinder _inner;
    private IMessageHandlerDefinition[]? _messageHandlerDefinitions;

    public CacheMessageHandlersFinder(IMessageHandlersFinder inner)
    {
        _inner = inner;
    }

    public IMessageHandlerDefinition[] FindDefinitions()
    {
        return _messageHandlerDefinitions ??= _inner.FindDefinitions();
    }
}
