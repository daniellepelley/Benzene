using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Builds and caches a topic-id-keyed index over every registered <see cref="IMessageHandlersFinder"/>'s
/// definitions, so <see cref="MessageHandlerDefinitionLookUp"/> (constructed fresh per message, per its
/// scoped DI lifetime) doesn't re-aggregate and re-deduplicate the full definition set on every
/// dispatch. Registered as a singleton, so the built index is shared across every scope for the
/// lifetime of the process.
/// </summary>
/// <remarks>
/// The index is rebuilt only when <see cref="MessageHandlersList.Version"/> has changed since it was
/// last built - i.e. a definition was added at runtime via <see cref="IMessageHandlersList.Add"/> after
/// the index was cached. Reflection- and DI-discovered definitions are fixed once the DI container is
/// built and never trigger a rebuild on their own.
/// </remarks>
public class MessageHandlerDefinitionIndex
{
    private static readonly IMessageHandlerDefinition[] Empty = Array.Empty<IMessageHandlerDefinition>();

    private readonly IEnumerable<IMessageHandlersFinder> _messageHandlersFinders;
    private readonly MessageHandlersList? _messageHandlersList;
    private readonly object _buildLock = new();

    // The built index and the version it was built for are published together as a single immutable
    // state object through one volatile reference. A separate reference + int would let a lock-free
    // reader on a weak memory model (e.g. ARM64/Graviton) observe the published dictionary reference
    // before the dictionary's own contents - or the matching version - became visible.
    private volatile IndexState? _state;

    private sealed record IndexState(Dictionary<string, IMessageHandlerDefinition[]> ByTopicId, int Version);

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHandlerDefinitionIndex"/> class.
    /// </summary>
    /// <param name="messageHandlersFinders">Every registered finder to aggregate definitions from.</param>
    /// <param name="messageHandlersList">
    /// The mutable list of explicitly-added definitions, used only to detect runtime additions that
    /// should trigger a rebuild. Optional (defaults to <c>null</c>) so the index can be constructed
    /// directly, without DI, wherever runtime mutation isn't a concern (e.g. tests) - in that case the
    /// index is built once and never invalidated.
    /// </param>
    public MessageHandlerDefinitionIndex(IEnumerable<IMessageHandlersFinder> messageHandlersFinders, MessageHandlersList? messageHandlersList = null)
    {
        _messageHandlersFinders = messageHandlersFinders;
        _messageHandlersList = messageHandlersList;
    }

    /// <summary>
    /// Returns every definition registered for the given topic id, de-duplicated by (id, version) -
    /// the version-selection itself is left to the caller (see <see cref="MessageHandlerDefinitionLookUp"/>).
    /// </summary>
    /// <param name="topicId">The topic id to look up.</param>
    /// <returns>Every definition registered for the topic id, or an empty array if none are registered.</returns>
    public IMessageHandlerDefinition[] GetByTopicId(string topicId)
    {
        return GetIndex().TryGetValue(topicId, out var definitions) ? definitions : Empty;
    }

    /// <summary>
    /// Returns every registered definition, de-duplicated by (id, version).
    /// </summary>
    /// <returns>Every registered handler definition.</returns>
    public IMessageHandlerDefinition[] GetAll()
    {
        return GetIndex().Values.SelectMany(x => x).ToArray();
    }

    private Dictionary<string, IMessageHandlerDefinition[]> GetIndex()
    {
        var currentVersion = _messageHandlersList?.Version ?? 0;

        var state = _state;
        if (state != null && state.Version == currentVersion)
        {
            return state.ByTopicId;
        }

        lock (_buildLock)
        {
            currentVersion = _messageHandlersList?.Version ?? 0;
            state = _state;
            if (state != null && state.Version == currentVersion)
            {
                return state.ByTopicId;
            }

            var built = _messageHandlersFinders
                .SelectMany(x => x.FindDefinitions())
                .GroupBy(x => (x.Topic.Id, x.Topic.Version))
                .Select(x => x.First())
                .GroupBy(x => x.Topic.Id)
                .ToDictionary(x => x.Key, x => x.ToArray());

            _state = new IndexState(built, currentVersion);
            return built;
        }
    }
}
