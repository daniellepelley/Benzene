﻿using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

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
        var contextOut = await _converter.CreateRequestAsync(context);
        await _middlewarePipeline.HandleAsync(contextOut, _serviceResolver);
        await _converter.MapResponseAsync(context, contextOut);
    }
}