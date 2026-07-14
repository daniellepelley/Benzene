using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IMessageRouterBuilder"/> implementation, passed to the configuration callback
/// given to the <c>router</c>-overloads of <see cref="MiddlewarePipelineExtensions"/>'s
/// <c>UseMessageHandlers</c> extension methods, so application code can register additional
/// <see cref="IHandlerMiddlewareBuilder"/>s and DI registrations for message-handler dispatch.
/// </summary>
public class MessageRouterBuilder : IMessageRouterBuilder
{
    private readonly Action<Action<IBenzeneServiceContainer>> _register;
    private readonly List<IHandlerMiddlewareBuilder> _builders;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageRouterBuilder"/> class.
    /// </summary>
    /// <param name="builders">The handler middleware builders already registered.</param>
    /// <param name="register">Delegate used to defer DI registrations to the owning application builder.</param>
    public MessageRouterBuilder(IEnumerable<IHandlerMiddlewareBuilder> builders,
        Action<Action<IBenzeneServiceContainer>> register)
    {
        _register = register;
        _builders = builders.ToList();
    }

    /// <inheritdoc />
    public IMessageRouterBuilder Add(IHandlerMiddlewareBuilder handlerMiddlewareBuilder)
    {
        _builders.Add(handlerMiddlewareBuilder);
        return this;
    }

    /// <inheritdoc />
    public IHandlerMiddlewareBuilder[] GetBuilders()
    {
        return _builders.ToArray();
    }

    /// <summary>
    /// Defers a DI registration action to the owning application builder.
    /// </summary>
    /// <param name="action">The registration action to run against the service container.</param>
    public void Register(Action<IBenzeneServiceContainer> action)
    {
        _register(action);
    }
}
