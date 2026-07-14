using Benzene.Abstractions.Messages;

namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// Metadata describing one discovered message handler: which topic it handles, and its request,
/// response, and handler CLR types. Produced by an <see cref="IMessageHandlersFinder"/> (e.g. via
/// reflection or DI scanning) and looked up by topic through <see cref="IMessageHandlerDefinitionLookUp"/>
/// so a router can resolve and create the handler (see <see cref="IMessageHandlerFactory"/>) without
/// scanning assemblies on every message.
/// </summary>
public interface IMessageHandlerDefinition : IRequestResponseMessageDefinition
{
    /// <summary>The concrete CLR type implementing the handler (e.g. an <see cref="IMessageHandler{TRequest, TResponse}"/>).</summary>
    Type HandlerType { get; }
}