using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Clients;

// Mirrors the shape Benzene.CodeGen.Client's generated clients emit: a "*Routing" static class
// with a public static string[] RequiredTopics field, reflected over at startup.
public static class OrderServiceClientRouting
{
    public static readonly string[] RequiredTopics = { "order:create", "order:cancel" };
}

public class ValidateOutboundRoutingTest
{
    [Fact]
    public void ValidateOutboundRouting_AllRequiredTopicsRouted_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route("order:create", pipeline => pipeline.OnRequest(_ => { }))
            .Route("order:cancel", pipeline => pipeline.OnRequest(_ => { })));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());

        var exception = Record.Exception(() => resolver.ValidateOutboundRouting());

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateOutboundRouting_MissingRequiredTopic_ThrowsMissingOutboundRoutesException()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route("order:create", pipeline => pipeline.OnRequest(_ => { })));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());

        var exception = Assert.Throws<MissingOutboundRoutesException>(() => resolver.ValidateOutboundRouting());

        Assert.Contains("order:cancel", exception.MissingTopics);
        Assert.DoesNotContain("order:create", exception.MissingTopics);
    }
}
