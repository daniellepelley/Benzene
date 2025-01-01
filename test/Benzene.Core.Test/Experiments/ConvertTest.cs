using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Experiments;

public class ConvertTest
{
    [Fact]
    public async Task Convert()
    {
        var services = new MicrosoftBenzeneServiceContainer(new ServiceCollection());
        var appBuilder = new MiddlewarePipelineBuilder<string>(services);
        var appBuilder2 = new MiddlewarePipelineBuilder<StringConvert>(services);
        var app = appBuilder
            .Use(resolver => new DemoConvertMiddleware(resolver, appBuilder2.Build()))
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

public class DemoConvertMiddleware : ContextConverterMiddleware<string, StringConvert>
{
    public DemoConvertMiddleware(IServiceResolver serviceResolver, IMiddlewarePipeline<StringConvert> middlewarePipeline)
        :base(new InlineContextConverter<string, StringConvert>(x => new StringConvert{ Value = x }, (s, content) => { }), middlewarePipeline, serviceResolver)
    {
    }
}

public class InlineContextConverter<TContextIn, TContextOut> : IContextConverter<TContextIn, TContextOut>
{
    private readonly Func<TContextIn, TContextOut> _createContextFunc;
    private readonly Action<TContextIn, TContextOut> _mapContext;

    public InlineContextConverter(Func<TContextIn, TContextOut> createContextFunc, Action<TContextIn, TContextOut> mapContext)
    {
        _mapContext = mapContext;
        _createContextFunc = createContextFunc;
    }

    public TContextOut CreateRequest(TContextIn contextIn) => _createContextFunc(contextIn);

    public void MapResponse(TContextIn contextIn, TContextOut contextOut) => _mapContext(contextIn, contextOut);
}

