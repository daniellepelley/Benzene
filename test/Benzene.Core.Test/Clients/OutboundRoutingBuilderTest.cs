using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Clients;

public class OutboundRoutingBuilderTest
{
    private static OutboundRoutingBuilder CreateBuilder()
    {
        return new OutboundRoutingBuilder(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));
    }

    [Fact]
    public void Build_DistinctTopics_ReturnsOnePipelinePerTopic()
    {
        var builder = CreateBuilder();

        builder
            .Route("order:create", pipeline => pipeline.OnRequest(_ => { }))
            .Route("audit:log", pipeline => pipeline.OnRequest(_ => { }));

        var routes = builder.Build();

        Assert.Equal(2, routes.Count);
        Assert.Contains("order:create", routes.Keys);
        Assert.Contains("audit:log", routes.Keys);
    }

    [Fact]
    public void Build_DuplicateTopic_ThrowsDuplicateOutboundRouteException()
    {
        var builder = CreateBuilder();

        builder
            .Route("order:create", pipeline => pipeline.OnRequest(_ => { }))
            .Route("order:create", pipeline => pipeline.OnRequest(_ => { }));

        var exception = Assert.Throws<DuplicateOutboundRouteException>(() => builder.Build());
        Assert.Equal("order:create", exception.Topic);
    }

    [Fact]
    public void Build_NoRoutes_ReturnsEmptyTable()
    {
        var builder = CreateBuilder();

        var routes = builder.Build();

        Assert.Empty(routes);
    }
}
