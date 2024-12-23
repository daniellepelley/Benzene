using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Experiments;

public class MiddlewareTest
{
    [Fact]
    public async Task Convert()
    {
        var services = new MicrosoftBenzeneServiceContainer(new ServiceCollection());
        var appBuilder = new MiddlewarePipelineBuilder<string>(services);
        var appBuilder2 = new MiddlewarePipelineBuilder<StringConvert>(services);
        var app = appBuilder
            .Use(resolver => new ConvertMiddleware2(resolver, appBuilder2.Build()))
            .Build();

        var entryPoint =
            new EntryPointMiddlewareApplication<string>(new MiddlewareApplication<string, string>(app, s => s),
                services.CreateServiceResolverFactory());

        await entryPoint.SendAsync("foo");
    }
}

public class StringConvert
{
    public string Value { get; set; }
}

public class MiddlewareBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly IMiddlewarePipeline<BenzeneMessageContext> _middlewarePipeline;
    private readonly IServiceResolver _serviceResolver;
    private readonly ISerializer _serializer;

    public MiddlewareBenzeneMessageClient(
        IMiddlewarePipeline<BenzeneMessageContext> middlewarePipeline, 
        ISerializer serializer,
        IServiceResolver serviceResolver)
    {
        _serializer = serializer;
        _serviceResolver = serviceResolver;
        _middlewarePipeline = middlewarePipeline;
    }

    public void Dispose()
    {
    }

    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        await _middlewarePipeline.HandleAsync(context, _serviceResolver);
        return BenzeneResult.Set(BenzeneResultStatus.Ok, _serializer.Deserialize<TResponse>(context.BenzeneMessageResponse.Body));
    }
}

public class ConvertMiddleware<TContextIn, TContextOut> : IMiddleware<TContextIn>
{
    private readonly IMiddlewarePipeline<TContextOut> _middlewarePipeline;
    private readonly IServiceResolver _serviceResolver;
    private readonly Func<TContextIn, TContextOut> _createContextFunc;
    private readonly Action<TContextIn, TContextOut> _mapContext;
    public string Name { get; }

    public ConvertMiddleware(
        IServiceResolver serviceResolver,
        IMiddlewarePipeline<TContextOut> middlewarePipeline,
        Func<TContextIn, TContextOut> createContextFunc,
        Action<TContextIn, TContextOut> mapContext)
    {
        _mapContext = mapContext;
        _createContextFunc = createContextFunc;
        _serviceResolver = serviceResolver;
        _middlewarePipeline = middlewarePipeline;
    }

    public async Task HandleAsync(TContextIn context, Func<Task> next)
    {
        var context2 = _createContextFunc(context);
        await _middlewarePipeline.HandleAsync(context2, _serviceResolver);
        _mapContext(context, context2);
    }
}

public class ConvertMiddleware2 : ConvertMiddleware<string, StringConvert>
{
    private IMiddlewarePipeline<StringConvert> _middlewarePipeline;
    private IServiceResolver _serviceResolver;
    public string Name { get; }

    public ConvertMiddleware2(IServiceResolver serviceResolver, IMiddlewarePipeline<StringConvert> middlewarePipeline)
        :base(serviceResolver, middlewarePipeline, x => new StringConvert{ Value = x }, (s, content) => { })
    {
        _serviceResolver = serviceResolver;
        _middlewarePipeline = middlewarePipeline;
    }

    public async Task HandleAsync(string context, Func<Task> next)
    {
        var context2 = new StringConvert { Value = context };
        await _middlewarePipeline.HandleAsync(context2, _serviceResolver);
        context = context2.Value;
    }
}

