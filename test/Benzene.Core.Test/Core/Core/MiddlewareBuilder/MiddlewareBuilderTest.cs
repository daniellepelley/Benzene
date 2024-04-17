using System.Threading.Tasks;
using Benzene.Core.DI;
using Benzene.Core.DirectMessage;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Core.MiddlewareBuilder;

public class MiddlewareBuilderTest
{
    [Fact]
    public async Task CreatePipeline_OnResponse()
    {
        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzene());

        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<DirectMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        var items = middlewarePipelineBuilder
                .OnResponse("Foo", x =>
                {
                    x.DirectMessageResponse = new DirectMessageResponse();
                    x.DirectMessageResponse.Message = "foo";
                })
            .GetItems();

        Assert.Single(items);

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var context = DirectMessageContext.CreateInstance(new DirectMessageRequest());
        await middlewarePipelineBuilder.AsPipeline().HandleAsync(context, serviceResolver);

        Assert.Equal("foo", context.DirectMessageResponse.Message);
    }
    
    [Fact]
    public async Task CreatePipeline_MiddlewareNames()
    {
        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzene());

        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<DirectMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        var items = middlewarePipelineBuilder
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
                    context.DirectMessageResponse = new DirectMessageResponse();
                    context.DirectMessageResponse.Message = "foo";
                })
            .GetItems();

        Assert.Equal(12, items.Length);
        
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        foreach (var item in items)
        {
            Assert.Equal(Benzene.Core.Constants.Unnamed, item(serviceResolver).Name);
        }
        
        var context = DirectMessageContext.CreateInstance(new DirectMessageRequest());
        await middlewarePipelineBuilder.AsPipeline().HandleAsync(context, serviceResolver);

        Assert.Equal("foo", context.DirectMessageResponse.Message);
    }

}
