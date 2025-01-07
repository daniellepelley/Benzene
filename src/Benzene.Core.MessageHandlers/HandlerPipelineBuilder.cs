using System.Diagnostics;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Core.MessageHandlers;

public class HandlerPipelineBuilder : IHandlerPipelineBuilder
{
    private readonly List<IHandlerMiddlewareBuilder> _routerMiddlewareBuilders;

    public HandlerPipelineBuilder(IEnumerable<IHandlerMiddlewareBuilder> routerMiddlewareBuilders)
    {
        _routerMiddlewareBuilders = routerMiddlewareBuilders.ToList();
    }

    public void Add(params IHandlerMiddlewareBuilder[] routerMiddlewareBuilders)
    {
        _routerMiddlewareBuilders.AddRange(routerMiddlewareBuilders);
    }

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