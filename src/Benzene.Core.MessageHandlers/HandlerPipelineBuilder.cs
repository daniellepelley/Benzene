using System.Diagnostics;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IHandlerPipelineBuilder"/> implementation: assembles a handler's middleware
/// pipeline from every registered <see cref="IHandlerMiddlewareBuilder"/> (e.g. filters), followed by
/// a terminal <see cref="MessageHandlerMiddleware{TRequest,TResponse}"/> that invokes the handler itself.
/// </summary>
public class HandlerPipelineBuilder : IHandlerPipelineBuilder
{
    private readonly List<IHandlerMiddlewareBuilder> _routerMiddlewareBuilders;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerPipelineBuilder"/> class.
    /// </summary>
    /// <param name="routerMiddlewareBuilders">The handler middleware builders to include in every pipeline built from now on.</param>
    public HandlerPipelineBuilder(IEnumerable<IHandlerMiddlewareBuilder> routerMiddlewareBuilders)
    {
        _routerMiddlewareBuilders = routerMiddlewareBuilders.ToList();
    }

    /// <inheritdoc />
    public void Add(params IHandlerMiddlewareBuilder[] routerMiddlewareBuilders)
    {
        _routerMiddlewareBuilders.AddRange(routerMiddlewareBuilders);
    }

    /// <summary>
    /// Builds the middleware pipeline for <paramref name="messageHandler"/>: asks each registered
    /// <see cref="IHandlerMiddlewareBuilder"/> to contribute middleware (skipping any that return
    /// <c>null</c> or are themselves <c>null</c>), then appends the handler invocation as the final step.
    /// </summary>
    /// <typeparam name="TRequest">The strongly-typed request handled by the pipeline.</typeparam>
    /// <typeparam name="TResponse">The strongly-typed response produced by the pipeline.</typeparam>
    /// <param name="messageHandler">The handler to invoke at the end of the pipeline.</param>
    /// <param name="serviceResolver">Resolver passed to each <see cref="IHandlerMiddlewareBuilder"/>.</param>
    /// <returns>The assembled pipeline.</returns>
    public IMiddlewarePipeline<IMessageHandlerContext<TRequest, TResponse>> Create<TRequest, TResponse>(
        IMessageHandler<TRequest, TResponse> messageHandler, IServiceResolver serviceResolver)
        where TRequest : class
    {
        var items = new List<IMiddleware<IMessageHandlerContext<TRequest, TResponse>>>();
        foreach (var routerMiddlewareBuilder in _routerMiddlewareBuilders)
        {
            if (routerMiddlewareBuilder == null)
            {
                Debug.WriteLine("Null IHandlerMiddlewareBuilder found");
                continue;
            }

            var middleware = routerMiddlewareBuilder.Create(serviceResolver, messageHandler);

            if (middleware != null)
            {
                items.Add(middleware);
            }
        }

        items.Add(new MessageHandlerMiddleware<TRequest, TResponse>(messageHandler));

        return new MiddlewarePipeline<IMessageHandlerContext<TRequest, TResponse>>(items
            .Select(x => new Func<IServiceResolver, IMiddleware<IMessageHandlerContext<TRequest, TResponse>>>(_ => x)).ToArray());
    }
}
