using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Benzene.Clients;
using Benzene.Clients.Aws.EventBridge;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients.Aws.EventBridge;

public class OutboundEventBridgeContextConverterTest
{
    private const string Source = "orders-api";

    private static Mock<IAmazonEventBridge> MockEventBridge()
    {
        var mock = new Mock<IAmazonEventBridge>();
        mock.Setup(x => x.PutEventsAsync(It.IsAny<PutEventsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutEventsResponse { HttpStatusCode = HttpStatusCode.OK, FailedEntryCount = 0 });
        return mock;
    }

    private static IBenzeneMessageSender SenderRoutedThrough(Mock<IAmazonEventBridge> mock,
        System.Action<Benzene.Clients.OutboundRoutingBuilder> route)
    {
        var services = new ServiceCollection();
        services.AddSingleton(mock.Object);
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(route);
        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        return resolver.GetService<IBenzeneMessageSender>();
    }

    [Fact]
    public async Task SendAsync_RoutedThroughUseEventBridge_PublishesWithTopicAsDetailTypeAndReturnsVoidResult()
    {
        var mock = MockEventBridge();
        var sender = SenderRoutedThrough(mock, routing => routing
            .Route(Defaults.Topic, pipeline => pipeline.UseEventBridge(Source)));

        var result = await sender.SendAsync<ExampleRequestPayload, Void>(
            Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" });

        mock.Verify(x => x.PutEventsAsync(
            It.Is<PutEventsRequest>(r =>
                r.Entries.Count == 1 &&
                r.Entries[0].Source == Source &&
                r.Entries[0].DetailType == Defaults.Topic &&
                JObject.Parse(r.Entries[0].Detail)["name"].ToString() == "foo"
            ), It.IsAny<CancellationToken>()));

        // EventBridge's fire-and-acknowledge result maps a clean PutEvents to Accepted.
        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }

    [Fact]
    public async Task SendAsync_WithCustomBusName_TargetsThatBus()
    {
        var mock = MockEventBridge();
        var sender = SenderRoutedThrough(mock, routing => routing
            .Route(Defaults.Topic, pipeline => pipeline.UseEventBridge(Source, eventBusName: "benzene-mesh-bus")));

        await sender.SendAsync<ExampleRequestPayload, Void>(
            Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" });

        mock.Verify(x => x.PutEventsAsync(
            It.Is<PutEventsRequest>(r => r.Entries[0].EventBusName == "benzene-mesh-bus"),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SendAsync_OnDefaultBus_DoesNotSetEventBusName()
    {
        var mock = MockEventBridge();
        var sender = SenderRoutedThrough(mock, routing => routing
            .Route(Defaults.Topic, pipeline => pipeline.UseEventBridge(Source)));

        await sender.SendAsync<ExampleRequestPayload, Void>(
            Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" });

        mock.Verify(x => x.PutEventsAsync(
            It.Is<PutEventsRequest>(r => string.IsNullOrEmpty(r.Entries[0].EventBusName)),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SendAsync_WithHeaders_EmbedsThemIntoDetailUnderBenzeneHeadersKey()
    {
        // EventBridge has no per-message attributes, so Benzene headers ride inside Detail under the
        // reserved _benzeneHeaders key - the inbound binding lifts them back out.
        var mock = MockEventBridge();
        var sender = SenderRoutedThrough(mock, routing => routing
            .Route(Defaults.Topic, pipeline => pipeline.UseEventBridge(Source)));

        await sender.SendAsync<ExampleRequestPayload, Void>(
            Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" },
            new Dictionary<string, string> { { "tenantId", "tenant-1" } });

        mock.Verify(x => x.PutEventsAsync(
            It.Is<PutEventsRequest>(r =>
                JObject.Parse(r.Entries[0].Detail)[OutboundEventBridgeContextConverter.EmbeddedHeadersKey]["tenantId"].ToString() == "tenant-1"
            ), It.IsAny<CancellationToken>()));
    }
}
