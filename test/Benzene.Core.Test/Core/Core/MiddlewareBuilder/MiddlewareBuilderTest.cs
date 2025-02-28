using System.Threading.Tasks;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Benzene.Test.Core.Core.MiddlewareBuilder;

public class MiddlewareBuilderTest
{
    [Fact]
    public async Task CreatePipeline_OnResponse()
    {
        var services = new ServiceCollection();
        services.TryAddSingleton<IMiddlewareFactory, DefaultMiddlewareFactory>();
        
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        var items = ((MiddlewarePipelineBuilder<BenzeneMessageContext>)middlewarePipelineBuilder
                .OnResponse("Foo", x =>
                {
                    x.BenzeneMessageResponse = new BenzeneMessageResponse
                    {
                        Body = "foo"
                    };
                }))
            .GetItems();

        Assert.Single(items);

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        await middlewarePipelineBuilder.Build().HandleAsync(context, serviceResolver);

        Assert.Equal("foo", context.BenzeneMessageResponse.Body);
    }
    
    [Fact]
    public async Task CreatePipeline_MiddlewareNames()
    {
        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzene());

        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        var items = ((MiddlewarePipelineBuilder<BenzeneMessageContext>)middlewarePipelineBuilder
                .Use(async (_, next) => await next())
                .Use(string.Empty, async (_, next) => await next())
                .Use(async (_, _, next) => await next())
                .Use("", async (_, _, next) => await next())
                .OnRequest(_ => { })
                .OnRequest(null, _ => { })
                .OnRequest((_,_) => { })
                .OnRequest(null, (_,_) => { })
                .OnResponse(_ => { })
                .OnResponse(null, _ => { })
                .OnResponse((_,_) => { })
                .OnResponse(null, (_,context) => {
                    context.BenzeneMessageResponse = new BenzeneMessageResponse();
                    context.BenzeneMessageResponse.Body = "foo";
                }))
            .GetItems();

        Assert.Equal(12, items.Length);
        
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        foreach (var item in items)
        {
            Assert.Equal(Benzene.Core.Constants.Unnamed, item(serviceResolver).Name);
        }
        
        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        await middlewarePipelineBuilder.Build().HandleAsync(context, serviceResolver);

        Assert.Equal("foo", context.BenzeneMessageResponse.Body);
    }
}
