using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Clients;
using Benzene.Clients.Aws.EventBridge;
using Benzene.Core.Middleware;
using Benzene.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Aws.EventBridge;

public class EventBridgeClientTest
{
    private class ExamplePayload
    {
        public string Name { get; set; }
    }

    [Fact]
    public async Task Converter_MapsTopicToDetailTypeAndEmbedsHeaders()
    {
        var converter = new EventBridgeContextConverter<ExamplePayload>("com.example.orders", "my-bus", new Benzene.Clients.JsonSerializer());
        var request = new BenzeneClientRequest<ExamplePayload>("order.created", new ExamplePayload { Name = "foo" },
            new Dictionary<string, string> { { "x-correlation-id", "abc-123" } });

        var context = await converter.CreateRequestAsync(new BenzeneClientContext<ExamplePayload, Void>(request));

        var entry = Assert.Single(context.Request.Entries);
        Assert.Equal("order.created", entry.DetailType);
        Assert.Equal("com.example.orders", entry.Source);
        Assert.Equal("my-bus", entry.EventBusName);

        using var detail = JsonDocument.Parse(entry.Detail);
        Assert.Equal("foo", detail.RootElement.GetProperty("name").GetString());
        Assert.Equal("abc-123", detail.RootElement.GetProperty("_benzeneHeaders").GetProperty("x-correlation-id").GetString());
    }

    [Fact]
    public async Task Converter_WithoutHeaders_DoesNotEmbedTheReservedKey()
    {
        var converter = new EventBridgeContextConverter<ExamplePayload>("com.example.orders");
        var request = new BenzeneClientRequest<ExamplePayload>("order.created", new ExamplePayload { Name = "foo" },
            new Dictionary<string, string>());

        var context = await converter.CreateRequestAsync(new BenzeneClientContext<ExamplePayload, Void>(request));

        var entry = Assert.Single(context.Request.Entries);
        using var detail = JsonDocument.Parse(entry.Detail);
        Assert.False(detail.RootElement.TryGetProperty("_benzeneHeaders", out _));
    }

    [Fact]
    public void ResultMapper_OkWithNoFailedEntries_MapsToAccepted()
    {
        var result = EventBridgeResultMapper.Map<Void>(new PutEventsResponse
        {
            HttpStatusCode = HttpStatusCode.OK,
            FailedEntryCount = 0
        });

        Assert.True(result.IsSuccessful);
        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }

    [Fact]
    public void ResultMapper_FailedEntry_MapsToServiceUnavailableWithErrorDetail()
    {
        var result = EventBridgeResultMapper.Map<Void>(new PutEventsResponse
        {
            HttpStatusCode = HttpStatusCode.OK,
            FailedEntryCount = 1,
            Entries = new List<PutEventsResultEntry>
            {
                new PutEventsResultEntry { ErrorCode = "ThrottlingException", ErrorMessage = "slow down" }
            }
        });

        Assert.False(result.IsSuccessful);
        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
        Assert.Contains("ThrottlingException", result.Errors[0]);
    }

    [Fact]
    public async Task Client_PublishesAndReturnsAccepted()
    {
        PutEventsRequest captured = null;
        var mockEventBridge = new Mock<IAmazonEventBridge>();
        mockEventBridge
            .Setup(x => x.PutEventsAsync(It.IsAny<PutEventsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutEventsRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(new PutEventsResponse { HttpStatusCode = HttpStatusCode.OK, FailedEntryCount = 0 });

        var client = new EventBridgeBenzeneMessageClient("com.example.orders", mockEventBridge.Object,
            NullLogger<EventBridgeBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<ExamplePayload, Void>(
            new BenzeneClientRequest<ExamplePayload>("order.created", new ExamplePayload { Name = "foo" },
                new Dictionary<string, string> { { "x-correlation-id", "abc-123" } }));

        Assert.True(result.IsSuccessful);
        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
        Assert.NotNull(captured);
        Assert.Equal("order.created", captured.Entries.Single().DetailType);
        Assert.Contains("_benzeneHeaders", captured.Entries.Single().Detail);
    }

    [Fact]
    public async Task Client_WhenPutEventsThrows_ReturnsServiceUnavailable()
    {
        var mockEventBridge = new Mock<IAmazonEventBridge>();
        mockEventBridge
            .Setup(x => x.PutEventsAsync(It.IsAny<PutEventsRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonEventBridgeException("boom"));

        var client = new EventBridgeBenzeneMessageClient("com.example.orders", mockEventBridge.Object,
            NullLogger<EventBridgeBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<ExamplePayload, Void>(
            new BenzeneClientRequest<ExamplePayload>("order.created", new ExamplePayload { Name = "foo" },
                new Dictionary<string, string>()));

        Assert.False(result.IsSuccessful);
        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }
}
