using System.Collections.Generic;
using System.Text;
using Benzene.RabbitMq.RabbitMqMessage;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace Benzene.Test.RabbitMq;

public class RabbitMqGettersTest
{
    private static RabbitMqContext CreateContext(string routingKey = "routing.key",
        IDictionary<string, object?>? headers = null, byte[]? body = null)
    {
        var properties = new BasicProperties { Headers = headers };
        var args = new BasicDeliverEventArgs(
            consumerTag: "tag",
            deliveryTag: 1,
            redelivered: false,
            exchange: "exchange",
            routingKey: routingKey,
            properties: properties,
            body: body ?? Encoding.UTF8.GetBytes("{\"value\":1}"));
        return RabbitMqContext.CreateInstance(args);
    }

    [Fact]
    public void TopicGetter_PrefersTopicHeader_OverRoutingKey()
    {
        var headers = new Dictionary<string, object?> { ["topic"] = Encoding.UTF8.GetBytes("orderCreated") };
        var context = CreateContext(routingKey: "some.routing.key", headers: headers);

        var topic = new RabbitMqMessageTopicGetter().GetTopic(context);

        Assert.Equal("orderCreated", topic.Id);
    }

    [Fact]
    public void TopicGetter_AcceptsStringHeaderValue()
    {
        var headers = new Dictionary<string, object?> { ["topic"] = "orderShipped" };
        var context = CreateContext(headers: headers);

        var topic = new RabbitMqMessageTopicGetter().GetTopic(context);

        Assert.Equal("orderShipped", topic.Id);
    }

    [Fact]
    public void TopicGetter_FallsBackToRoutingKey_WhenNoTopicHeader()
    {
        var context = CreateContext(routingKey: "orderPlaced", headers: null);

        var topic = new RabbitMqMessageTopicGetter().GetTopic(context);

        Assert.Equal("orderPlaced", topic.Id);
    }

    [Fact]
    public void TopicGetter_ReadsCustomHeaderKey_WhenConfigured()
    {
        var headers = new Dictionary<string, object?> { ["x-my-topic"] = Encoding.UTF8.GetBytes("orderCreated") };
        var context = CreateContext(routingKey: "some.routing.key", headers: headers);

        var topic = new RabbitMqMessageTopicGetter("x-my-topic").GetTopic(context);

        Assert.Equal("orderCreated", topic.Id);
    }

    [Fact]
    public void TopicGetter_IgnoresDefaultHeader_WhenCustomKeyConfigured()
    {
        // A message carrying only the default "topic" header should fall back to the routing key when
        // the getter is configured to read a different header, proving the key is honored end-to-end.
        var headers = new Dictionary<string, object?> { ["topic"] = Encoding.UTF8.GetBytes("orderCreated") };
        var context = CreateContext(routingKey: "orderPlaced", headers: headers);

        var topic = new RabbitMqMessageTopicGetter("x-my-topic").GetTopic(context);

        Assert.Equal("orderPlaced", topic.Id);
    }

    [Fact]
    public void BodyGetter_DecodesUtf8Body()
    {
        var context = CreateContext(body: Encoding.UTF8.GetBytes("{\"name\":\"benzene\"}"));

        var body = new RabbitMqMessageBodyGetter().GetBody(context);

        Assert.Equal("{\"name\":\"benzene\"}", body);
    }

    [Fact]
    public void HeadersGetter_DecodesByteArrayAndStringValues()
    {
        var headers = new Dictionary<string, object?>
        {
            ["correlation-id"] = Encoding.UTF8.GetBytes("abc-123"),
            ["tenant"] = "acme",
        };
        var context = CreateContext(headers: headers);

        var result = new RabbitMqMessageHeadersGetter().GetHeaders(context);

        Assert.Equal("abc-123", result["correlation-id"]);
        Assert.Equal("acme", result["tenant"]);
    }

    [Fact]
    public void HeadersGetter_ReturnsEmpty_WhenNoHeaders()
    {
        var context = CreateContext(headers: null);

        var result = new RabbitMqMessageHeadersGetter().GetHeaders(context);

        Assert.Empty(result);
    }
}
