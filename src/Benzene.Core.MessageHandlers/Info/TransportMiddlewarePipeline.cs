using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.MessageHandlers.Info;

public class TransportMiddlewarePipeline<TContext> : IMiddlewarePipeline<TContext>
{
    private readonly IMiddlewarePipeline<TContext> _pipeline;
    private readonly string _transport;

    public TransportMiddlewarePipeline(string transport, IMiddlewarePipeline<TContext> pipeline)
    {
        _transport = transport;
        _pipeline = pipeline;
    }

    public Task HandleAsync(TContext context, IServiceResolver serviceResolver)
    {
        var setCurrentTransport = serviceResolver.GetService<ISetCurrentTransport>();
        setCurrentTransport.SetTransport(_transport);
        return _pipeline.HandleAsync(context, serviceResolver);
    }
}