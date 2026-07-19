using System.Collections.Concurrent;
using System.Linq.Expressions;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Exceptions;
using Benzene.Core.Messages;
using Microsoft.Extensions.Logging;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IMessageHandlerFactory"/> implementation: resolves the handler instance
/// described by an <see cref="IMessageHandlerDefinition"/> from DI, wraps it via the registered
/// <see cref="IMessageHandlerWrapper"/>, and adapts it to the non-generic <see cref="IMessageHandler"/>
/// surface the router invokes.
/// </summary>
/// <remarks>
/// Since the definition's request/response types are only known at runtime (as <see cref="Type"/>
/// values), <see cref="Create"/> invokes the generic
/// <see cref="CreateMessageHandlerByType{TMessageHandler, TRequest, TResponse}"/> method with the
/// definition's concrete type arguments via a compiled delegate, cached per distinct
/// (handler, request, response) type triple in <see cref="_dispatcherCache"/> — the reflection cost
/// (<c>MakeGenericMethod</c> + building the delegate) is paid once per triple, not once per message,
/// since the definition set is static after startup.
/// </remarks>
internal class MessageHandlerFactory : IMessageHandlerFactory
{
    private static readonly ConcurrentDictionary<(Type HandlerType, Type RequestType, Type ResponseType), Func<MessageHandlerFactory, ITopic, IMessageHandler?>> _dispatcherCache = new();
    private readonly IMessageHandlerWrapper _messageHandlerWrapper;
    private readonly IServiceResolver _serviceResolver;
    private readonly ILoggerFactory _loggerFactory;
    private IDefaultStatuses _defaultStatuses;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHandlerFactory"/> class.
    /// </summary>
    /// <param name="serviceResolver">Resolves the concrete handler instance from DI.</param>
    /// <param name="messageHandlerWrapper">Wraps the resolved handler with the registered handler pipeline/middleware.</param>
    /// <param name="loggerFactory">Creates the logger passed to each created <see cref="MessageHandler{TRequest,TResponse}"/>.</param>
    /// <param name="defaultStatuses">Supplies the status codes used for handler-level error results.</param>
    public MessageHandlerFactory(IServiceResolver serviceResolver, IMessageHandlerWrapper messageHandlerWrapper, ILoggerFactory loggerFactory, IDefaultStatuses defaultStatuses)
    {
        _defaultStatuses = defaultStatuses;
        _loggerFactory = loggerFactory;
        _serviceResolver = serviceResolver;
        _messageHandlerWrapper = messageHandlerWrapper;
    }

    /// <summary>
    /// Resolves and wraps the handler described by <paramref name="messageHandlerDefinition"/> into
    /// an invocable <see cref="IMessageHandler"/>.
    /// </summary>
    /// <param name="messageHandlerDefinition">The definition describing which handler type to create and its request/response types.</param>
    /// <returns>The wrapped, invocable handler, or <c>null</c> if the resolved service doesn't implement a recognized handler interface.</returns>
    public IMessageHandler Create(IMessageHandlerDefinition messageHandlerDefinition)
    {
        return CreateMessageHandler(new Topic(messageHandlerDefinition.Topic.Id, messageHandlerDefinition.Topic.Version), messageHandlerDefinition.HandlerType, messageHandlerDefinition.RequestType,
            messageHandlerDefinition.ResponseType);
    }

    private IMessageHandler CreateMessageHandler(ITopic topic, Type messageHandlerType, Type requestType, Type responseType)
    {
        var dispatcher = _dispatcherCache.GetOrAdd((messageHandlerType, requestType, responseType), BuildDispatcher);
        return dispatcher(this, topic);
    }

    /// <summary>
    /// Builds a compiled delegate equivalent to
    /// <c>(factory, topic) => factory.CreateMessageHandlerByType&lt;THandler, TRequest, TResponse&gt;(topic)</c>
    /// for a specific type triple, so subsequent dispatches for that triple avoid repeating
    /// <c>MakeGenericMethod</c> and reflective <c>Invoke</c>.
    /// </summary>
    private static Func<MessageHandlerFactory, ITopic, IMessageHandler?> BuildDispatcher(
        (Type HandlerType, Type RequestType, Type ResponseType) key)
    {
        var method = typeof(MessageHandlerFactory).GetMethod(nameof(CreateMessageHandlerByType));
        if (method == null)
        {
            throw new BenzeneException("Method CreateMessageHandlerByType is missing");
        }

        var genericMethod = method.MakeGenericMethod(key.HandlerType, key.RequestType, key.ResponseType);

        var factoryParameter = Expression.Parameter(typeof(MessageHandlerFactory), "factory");
        var topicParameter = Expression.Parameter(typeof(ITopic), "topic");
        var call = Expression.Call(factoryParameter, genericMethod, topicParameter);

        return Expression.Lambda<Func<MessageHandlerFactory, ITopic, IMessageHandler?>>(call, factoryParameter, topicParameter).Compile();
    }

    /// <summary>
    /// Resolves <typeparamref name="TMessageHandler"/> from DI, wraps it (whether it implements the
    /// request/response or the no-response handler interface) and returns it as an
    /// <see cref="IMessageHandler"/> ready to be invoked by the router.
    /// </summary>
    /// <remarks>
    /// This is public so it can be invoked reflectively (via <c>MakeGenericMethod</c>) from
    /// <see cref="Create"/>, which only knows the handler's types at runtime as <see cref="Type"/>
    /// values. It is not intended to be called directly by application code.
    /// </remarks>
    /// <typeparam name="TMessageHandler">The concrete handler type to resolve from DI.</typeparam>
    /// <typeparam name="TRequest">The handler's strongly-typed request type.</typeparam>
    /// <typeparam name="TResponse">The handler's strongly-typed response type.</typeparam>
    /// <param name="topic">The topic this handler is being created for.</param>
    /// <returns>The wrapped handler, or <c>null</c> if the resolved instance implements neither recognized handler interface.</returns>
    public IMessageHandler CreateMessageHandlerByType<TMessageHandler, TRequest, TResponse>(ITopic topic)
        where TMessageHandler : class
        where TRequest : class
    {
        var messageHandler = _serviceResolver.GetService<TMessageHandler>();
        var logger = _loggerFactory.CreateLogger(typeof(TMessageHandler));

        switch (messageHandler)
        {
            case IMessageHandler<TRequest, TResponse> handlerWithResponse:
            {
                var wrapped = _messageHandlerWrapper.Wrap(topic, handlerWithResponse);
                return new MessageHandler<TRequest, TResponse>(wrapped, logger, _defaultStatuses);
            }
            case IMessageHandler<TRequest> handlerNoResponse:
            {
                var wrapped = _messageHandlerWrapper.Wrap<TRequest, TResponse>(topic, handlerNoResponse);
                return new MessageHandler<TRequest, TResponse>(wrapped, logger, _defaultStatuses);
            }
            default:
                return null;
        }
    }
}
