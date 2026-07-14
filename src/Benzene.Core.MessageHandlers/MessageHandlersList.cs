using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// In-memory registry of handler definitions added explicitly (e.g. via <see cref="MiddlewarePipelineExtensions.AddMessageHandler{THandler,TRequest,TResponse}"/>),
/// as opposed to those discovered by reflection. Implements both <see cref="IMessageHandlersFinder"/>
/// (so it can be composed with other finders) and <see cref="IMessageHandlersList"/> (so definitions
/// can be added to it directly).
/// </summary>
public class MessageHandlersList : IMessageHandlersFinder, IMessageHandlersList
{
    private readonly List<IMessageHandlerDefinition> _list = new();

    /// <summary>
    /// A monotonically increasing stamp, incremented on every <see cref="Add"/>. Consulted by
    /// <see cref="MessageHandlerDefinitionIndex"/> to detect runtime additions that should invalidate
    /// its cached index - not part of <see cref="IMessageHandlersList"/> since it's an internal
    /// implementation detail of that caching, not a public list operation.
    /// </summary>
    public int Version { get; private set; }

    /// <inheritdoc />
    public IMessageHandlerDefinition[] FindDefinitions()
    {
        return _list.ToArray();
    }

    /// <summary>
    /// Adds a handler definition to the registry.
    /// </summary>
    /// <param name="messageHandlerDefinition">The handler definition to add.</param>
    public void Add(IMessageHandlerDefinition messageHandlerDefinition)
    {
        _list.Add(messageHandlerDefinition);
        Version++;
    }
}
