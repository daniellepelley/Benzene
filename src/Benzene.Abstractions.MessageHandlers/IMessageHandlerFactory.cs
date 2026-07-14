namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// Resolves and wraps the handler described by an <see cref="IMessageHandlerDefinition"/> (typically
/// via DI) into the non-generic <see cref="IMessageHandler"/> invocation surface a router can call
/// without knowing the handler's concrete request/response types.
/// </summary>
public interface IMessageHandlerFactory
{
    /// <summary>Creates the invokable handler instance for the given handler definition.</summary>
    /// <param name="messageHandlerDefinition">The definition describing which handler to create.</param>
    /// <returns>The handler, ready to be invoked via <see cref="IMessageHandler.HandlerAsync"/>.</returns>
    IMessageHandler Create(IMessageHandlerDefinition messageHandlerDefinition);
}