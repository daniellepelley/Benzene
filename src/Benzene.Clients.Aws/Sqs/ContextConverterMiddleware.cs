using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Clients.Aws.Common;

namespace Benzene.Clients.Aws.Sqs;

public class ContextConverterMiddleware<TContext, TContextOut> : IMiddleware<TContext>
{
    private readonly IContextConverter<TContext, TContextOut> _converter;
    private readonly IMiddlewarePipeline<TContextOut> _middlewarePipeline;
    private readonly IServiceResolver _serviceResolver;

    public ContextConverterMiddleware(IContextConverter<TContext, TContextOut> converter, IMiddlewarePipeline<TContextOut> middlewarePipeline, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _middlewarePipeline = middlewarePipeline;
        _converter = converter;
    }

    public string Name => "Convert";
    
    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var contextOut = _converter.CreateRequest(context);
        await _middlewarePipeline.HandleAsync(contextOut, _serviceResolver);
        _converter.MapResponse(context, contextOut); 
    }
}