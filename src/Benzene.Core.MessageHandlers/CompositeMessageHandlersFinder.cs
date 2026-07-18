using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Combines several <see cref="IMessageHandlersFinder"/>s (e.g. a reflection-based finder and a
/// DI-based <see cref="DependencyMessageHandlersFinder"/>) into a single finder whose definitions are
/// the union of all of them, so <see cref="MessageHandlerDefinitionLookUp"/> only needs to depend on one finder.
/// </summary>
internal class CompositeMessageHandlersFinder : IMessageHandlersFinder
{
    private readonly IMessageHandlersFinder[] _inners;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeMessageHandlersFinder"/> class.
    /// </summary>
    /// <param name="inners">The finders to combine.</param>
    public CompositeMessageHandlersFinder(params IMessageHandlersFinder[] inners)
    {
        _inners = inners;
    }

    /// <summary>
    /// Returns the union of every inner finder's definitions, without de-duplication.
    /// </summary>
    /// <returns>All handler definitions from every inner finder, concatenated.</returns>
    public IMessageHandlerDefinition[] FindDefinitions()
    {
        return _inners.SelectMany(x => x.FindDefinitions()).ToArray();
    }
}
