using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Clients;

// Mirrors the shape Benzene.CodeGen.Client's generated clients emit: a "*Routing" static class
// carrying [OutboundRoutingContract] with a public static string[] RequiredTopics field, reflected
// over at startup.
[OutboundRoutingContract]
public static class OrderServiceClientRouting
{
    public static readonly string[] RequiredTopics = { "order:create", "order:cancel" };
}

// Deliberately NOT attributed: a type with a RequiredTopics field but no
// [OutboundRoutingContract] must be ignored by the attribute-gated scan. Its topics
// ("phantom:*") never appear in any MissingOutboundRoutesException below.
public static class PhantomServiceClientRouting
{
    public static readonly string[] RequiredTopics = { "phantom:create", "phantom:cancel" };
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

    [Fact]
    public void ValidateOutboundRouting_UnattributedRoutingType_IsIgnored()
    {
        // PhantomServiceClientRouting has a RequiredTopics field but no [OutboundRoutingContract],
        // so the attribute-gated scan must not sweep its topics into the missing-route check.
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route("order:create", pipeline => pipeline.OnRequest(_ => { }))
            .Route("order:cancel", pipeline => pipeline.OnRequest(_ => { })));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());

        var exception = Record.Exception(() => resolver.ValidateOutboundRouting());

        Assert.Null(exception);
    }
}
