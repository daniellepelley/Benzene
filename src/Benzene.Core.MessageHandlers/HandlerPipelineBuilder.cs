using System.Diagnostics;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;
using Benzene.Core.Middleware;
using Benzene.Results;

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

    public IMiddlewarePipeline<IMessageContext<TRequest, TResponse>> Create<TRequest, TResponse>(
        IMessageHandler<TRequest, TResponse> messageHandler, IServiceResolver serviceResolver) 
        where TRequest : class
    {
        var items = new List<IMiddleware<IMessageContext<TRequest, TResponse>>>();
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
        
        return new MiddlewarePipeline<IMessageContext<TRequest, TResponse>>(items
            .Select(x => new Func<IServiceResolver, IMiddleware<IMessageContext<TRequest, TResponse>>>(_ => x)).ToArray());
    }
}

public abstract class MessageResultSetterBase<TContext>: IResultSetter<TContext> where TContext : IHasMessageResult
{
    public Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        context.MessageResult = new MessageResult(messageHandlerResult.Topic, messageHandlerResult.MessageHandlerDefinition, messageHandlerResult.Result.Status, messageHandlerResult.Result.IsSuccessful,
            messageHandlerResult.Result.PayloadAsObject, messageHandlerResult.Result.Errors);
        return Task.CompletedTask;
    }
}

public class ResponseMessageResultSetterBase<TContext> : IResultSetter<TContext> where TContext : class, IHasMessageResult
{
    private readonly IResponseHandlerContainer<TContext> _responseHandlerContainer;

    public ResponseMessageResultSetterBase(IResponseHandlerContainer<TContext> responseHandlerContainer)
    {
        _responseHandlerContainer = responseHandlerContainer;
    }

    public async Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        context.MessageResult = new MessageResult(messageHandlerResult.Topic, messageHandlerResult.MessageHandlerDefinition, messageHandlerResult.Result.Status, messageHandlerResult.Result.IsSuccessful,
            messageHandlerResult.Result.PayloadAsObject, messageHandlerResult.Result.Errors);
        await _responseHandlerContainer.HandleAsync(context, messageHandlerResult);
    }
}


