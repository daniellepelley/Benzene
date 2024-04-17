using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;

namespace Benzene.Core.Middleware;

public class MiddlewareMultiApplication<TEvent, TContext, TResult> : IMiddlewareApplication<TEvent, TResult[]>
    where TContext : IHasMessageResult
{
    private readonly Func<TEvent, TContext[]> _mapper;
    private readonly IMiddlewarePipeline<TContext> _pipelineBuilder;
    private readonly Func<TContext, TResult> _resultMapper;

    public MiddlewareMultiApplication(IMiddlewarePipeline<TContext> pipelineBuilder, Func<TEvent, TContext[]> mapper,
        Func<TContext, TResult> resultMapper)
    {
        _resultMapper = resultMapper;
        _mapper = mapper;
        _pipelineBuilder = pipelineBuilder;
    }

    public Task<TResult[]> HandleAsync(TEvent @event, IServiceResolver serviceResolver)
    {
        var tasks = _mapper(@event).Select(async context =>
            {
                using var scope = serviceResolver.GetService<IServiceResolverFactory>().CreateScope();
                await _pipelineBuilder.HandleAsync(context, scope);
                return _resultMapper(context);
            })
            .ToArray();

        return Task.WhenAll(tasks);
    }
}

public class MiddlewareMultiApplication<TEvent, TContext> : IMiddlewareApplication<TEvent>
{
    private readonly Func<TEvent, TContext[]> _mapper;
    private readonly IMiddlewarePipeline<TContext> _pipeline;
    private readonly string _transport;

    public MiddlewareMultiApplication(string transport, IMiddlewarePipeline<TContext> pipeline, Func<TEvent, TContext[]> mapper)
    {
        _transport = transport;
        _mapper = mapper;
        _pipeline = pipeline;
    }

    public Task HandleAsync(TEvent @event, IServiceResolver serviceResolver)
    {
        var tasks = _mapper(@event).Select(async context =>
            {
                using var scope = serviceResolver.GetService<IServiceResolverFactory>().CreateScope();
                var setCurrentTransport = scope.GetService<ISetCurrentTransport>();
                setCurrentTransport.SetTransport(_transport);
                await _pipeline.HandleAsync(context, scope);
            })
            .ToArray();

        return Task.WhenAll(tasks);
    }
}
