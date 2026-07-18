using System.Threading.Tasks;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

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

    [Fact]
    public async Task SendAsync_ResponseTypeMismatch_ThrowsOutboundResponseTypeMismatchExceptionNamingBothTypes()
    {
        // Simulates a send-acknowledgement-only transport (SQS/SNS/Service Bus/...) whose route
        // always sets an IBenzeneResult<Void> response, regardless of the caller's TResponse.
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route("order:create", pipeline => pipeline.OnRequest(context =>
                context.Response = BenzeneResult.Accepted<Void>())));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var sender = resolver.GetService<IBenzeneMessageSender>();

        var exception = await Assert.ThrowsAsync<OutboundResponseTypeMismatchException>(
            () => sender.SendAsync<string, string>("order:create", "payload"));

        Assert.Equal("order:create", exception.Topic);
        Assert.Equal(typeof(Void), exception.ActualResponseType);
        Assert.Equal(typeof(string), exception.RequestedResponseType);
        Assert.Contains("order:create", exception.Message);
        Assert.Contains("Void", exception.Message);
        Assert.Contains("String", exception.Message);
    }

    [Fact]
    public async Task SendAsync_ResponseTypeMatchesVoid_ReturnsSuccessfully()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route("order:create", pipeline => pipeline.OnRequest(context =>
                context.Response = BenzeneResult.Accepted<Void>())));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var sender = resolver.GetService<IBenzeneMessageSender>();

        var result = await sender.SendAsync<string, Void>("order:create", "payload");

        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }
}
