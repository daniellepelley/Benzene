using System.Threading.Tasks;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Clients;

// DefaultBenzeneMessageSender is internal (no InternalsVisibleTo wiring in this repo), so it's
// exercised here through its public surface: AddOutboundRouting -> resolved IBenzeneMessageSender.
public class DefaultBenzeneMessageSenderTest
{
    [Fact]
    public async Task SendAsync_RoutedTopic_RunsThatTopicsPipelineAndReturnsItsResponse()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route("order:create", pipeline => pipeline.OnRequest(context =>
                context.Response = BenzeneResult.Ok("created"))));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var sender = resolver.GetService<IBenzeneMessageSender>();

        var result = await sender.SendAsync<string, string>("order:create", "payload");

        Assert.Equal("created", result.Payload);
    }

    [Fact]
    public async Task SendAsync_TwoRoutedTopics_EachRunsItsOwnPipeline()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route("order:create", pipeline => pipeline.OnRequest(context =>
                context.Response = BenzeneResult.Ok("order-result")))
            .Route("audit:log", pipeline => pipeline.OnRequest(context =>
                context.Response = BenzeneResult.Ok("audit-result"))));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var sender = resolver.GetService<IBenzeneMessageSender>();

        var orderResult = await sender.SendAsync<string, string>("order:create", "payload");
        var auditResult = await sender.SendAsync<string, string>("audit:log", "payload");

        Assert.Equal("order-result", orderResult.Payload);
        Assert.Equal("audit-result", auditResult.Payload);
    }

    [Fact]
    public async Task SendAsync_UnroutedTopic_ThrowsUnroutedTopicException()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route("order:create", pipeline => pipeline.OnRequest(_ => { })));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var sender = resolver.GetService<IBenzeneMessageSender>();

        var exception = await Assert.ThrowsAsync<UnroutedTopicException>(
            () => sender.SendAsync<string, string>("unknown:topic", "payload"));
        Assert.Equal("unknown:topic", exception.Topic);
    }
}
