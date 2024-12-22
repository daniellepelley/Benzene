using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers;

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