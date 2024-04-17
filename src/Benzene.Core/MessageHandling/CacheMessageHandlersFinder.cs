using Benzene.Abstractions.MessageHandling;

namespace Benzene.Core.MessageHandling;

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
