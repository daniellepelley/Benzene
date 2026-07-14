using System.Text.Json;
using Benzene.Aws.Lambda.EventBridge;
using Xunit;

namespace Benzene.Test.Aws.EventBridge;

public class EventBridgeGettersTest
{
    private static EventBridgeContext CreateContext(string detailJson = "{\"name\":\"some-name\"}")
    {
        using var document = JsonDocument.Parse(detailJson);
        return new EventBridgeContext(new EventBridgeEvent
        {
            Version = "0",
            Id = "event-id-1",
            DetailType = "order.created",
            Source = "com.example.orders",
            Account = "123456789012",
            Region = "eu-west-1",
            Time = "2026-01-01T00:00:00Z",
            Detail = document.RootElement.Clone()
        });
    }

    [Fact]
    public void Topic_IsTheDetailType()
    {
        var topic = new EventBridgeMessageTopicGetter().GetTopic(CreateContext());

        Assert.Equal("order.created", topic.Id);
    }

    [Fact]
    public void Body_IsTheRawDetailJson()
    {
        var body = new EventBridgeMessageBodyGetter().GetBody(CreateContext("{\"name\":\"some-name\"}"));

        Assert.Equal("{\"name\":\"some-name\"}", body);
    }

    [Fact]
    public void Headers_ContainPrefixedEnvelopeMetadata()
    {
        var headers = new EventBridgeMessageHeadersGetter().GetHeaders(CreateContext());

        Assert.Equal("com.example.orders", headers["eventbridge-source"]);
        Assert.Equal("event-id-1", headers["eventbridge-id"]);
        Assert.Equal("123456789012", headers["eventbridge-account"]);
        Assert.Equal("eu-west-1", headers["eventbridge-region"]);
        Assert.Equal("order.created", headers["eventbridge-detail-type"]);
    }

    [Fact]
    public void Headers_LiftEmbeddedBenzeneHeadersFromDetail()
    {
        var context = CreateContext(
            "{\"name\":\"some-name\",\"_benzeneHeaders\":{\"x-correlation-id\":\"abc-123\",\"traceparent\":\"00-trace-span-01\"}}");

        var headers = new EventBridgeMessageHeadersGetter().GetHeaders(context);

        Assert.Equal("abc-123", headers["x-correlation-id"]);
        Assert.Equal("00-trace-span-01", headers["traceparent"]);
    }

    [Fact]
    public void Headers_WithoutEmbeddedHeaders_OnlyEnvelopeMetadataIsPresent()
    {
        var headers = new EventBridgeMessageHeadersGetter().GetHeaders(CreateContext());

        Assert.DoesNotContain(headers, x => !x.Key.StartsWith("eventbridge-"));
    }
}
