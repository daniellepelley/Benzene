using Benzene.Abstractions.Messages;

namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// Discovers <see cref="IMessageHandlerDefinition"/>s for registration into an
/// <see cref="IMessageHandlersList"/> at startup. Implementations vary by discovery strategy (e.g.
/// reflection over an assembly, resolution from DI, an in-memory cache wrapping another finder, or a
/// composite of several finders) and can be layered together.
/// </summary>
public interface IMessageHandlersFinder : IMessageDefinitionFinder<IMessageHandlerDefinition>
{
}