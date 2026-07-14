using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// <see cref="IMessageHandlersFinder"/> that returns whatever <see cref="IMessageHandlerDefinition"/>s
/// are registered directly in the DI container, so handlers can be registered explicitly (e.g. by a
/// library that ships its own handlers, without relying on reflection-based discovery).
/// </summary>
public class DependencyMessageHandlersFinder : IMessageHandlersFinder
{
    private readonly IEnumerable<IMessageHandlerDefinition> _messageHandlerDefinitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyMessageHandlersFinder"/> class.
    /// </summary>
    /// <param name="messageHandlerDefinitions">The handler definitions registered in DI.</param>
    public DependencyMessageHandlersFinder(IEnumerable<IMessageHandlerDefinition> messageHandlerDefinitions)
    {
        _messageHandlerDefinitions = messageHandlerDefinitions;
    }

    /// <inheritdoc />
    public IMessageHandlerDefinition[] FindDefinitions()
    {
        return _messageHandlerDefinitions.ToArray();
    }
}
