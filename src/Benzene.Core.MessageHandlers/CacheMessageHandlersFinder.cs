using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Decorates another <see cref="IMessageHandlersFinder"/>, caching the result of its first
/// <see cref="FindDefinitions"/> call so subsequent calls (e.g. per-request lookups against a
/// reflection-based finder) don't repeat potentially expensive assembly scanning.
/// </summary>
/// <remarks>
/// The cache is populated lazily on first use and never invalidated - handler definitions are
/// assumed not to change for the lifetime of the process.
/// </remarks>
internal class CacheMessageHandlersFinder : IMessageHandlersFinder
{
    private readonly IMessageHandlersFinder _inner;
    private IMessageHandlerDefinition[]? _messageHandlerDefinitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheMessageHandlersFinder"/> class.
    /// </summary>
    /// <param name="inner">The finder to cache the results of.</param>
    public CacheMessageHandlersFinder(IMessageHandlersFinder inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Returns the cached handler definitions, calling the inner finder only on the first invocation.
    /// </summary>
    /// <returns>The cached (or newly discovered, on first call) handler definitions.</returns>
    public IMessageHandlerDefinition[] FindDefinitions()
    {
        return _messageHandlerDefinitions ??= _inner.FindDefinitions();
    }
}
