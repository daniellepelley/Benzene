using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.RabbitMq.RabbitMqSendMessage;
using Benzene.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace Benzene.Test.RabbitMq;

public class RabbitMqBenzeneMessageClientTest
{
    private static Mock<IChannel> PublishingChannel()
    {
        var mockChannel = new Mock<IChannel>();
        mockChannel.Setup(x => x.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        return mockChannel;
    }

    [Fact]
    public async Task SendMessageAsync_PublishSucceeds_ReturnsAccepted()
    {
        var client = new RabbitMqBenzeneMessageClient(PublishingChannel().Object,
            NullLogger<RabbitMqBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<string, string>("some-topic", "some-message");

        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }

    [Fact]
    public async Task SendMessageAsync_ThrowingChannel_ReturnsServiceUnavailable()
    {
        var mockChannel = new Mock<IChannel>();
        mockChannel.Setup(x => x.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromException(new Exception("boom")));

        var client = new RabbitMqBenzeneMessageClient(mockChannel.Object,
            NullLogger<RabbitMqBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<string, string>("some-topic", "some-message");

        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }

    [Fact]
    public async Task SendMessageAsync_PublishesToTopicAsRoutingKey_AndForwardsHeaders()
    {
        string? capturedExchange = null;
        string? capturedRoutingKey = null;
        BasicProperties? capturedProps = null;

        var mockChannel = new Mock<IChannel>();
        mockChannel.Setup(x => x.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback((string exchange, string routingKey, bool _, BasicProperties props, ReadOnlyMemory<byte> _, CancellationToken _) =>
            {
                capturedExchange = exchange;
                capturedRoutingKey = routingKey;
                capturedProps = props;
            })
            .Returns(ValueTask.CompletedTask);

        var client = new RabbitMqBenzeneMessageClient(mockChannel.Object,
            NullLogger<RabbitMqBenzeneMessageClient>.Instance, new NullServiceResolver(), exchange: "events");

        var headers = new Dictionary<string, string> { ["correlation-id"] = "abc-123" };
        await client.SendMessageAsync<string, string>("orderCreated", "some-message", headers);

        Assert.Equal("events", capturedExchange);
        Assert.Equal("orderCreated", capturedRoutingKey);
        Assert.NotNull(capturedProps);
        Assert.NotNull(capturedProps!.Headers);
        Assert.Equal("abc-123", Encoding.UTF8.GetString((byte[])capturedProps.Headers!["correlation-id"]!));
        // The topic is forwarded as a header too, so a Benzene consumer's header-first topic getter round-trips it.
        Assert.Equal("orderCreated", Encoding.UTF8.GetString((byte[])capturedProps.Headers!["topic"]!));
    }

    [Fact]
    public async Task SendMessageAsync_PrebuiltPipeline_ReturnsAccepted()
    {
        var pipeline = new MiddlewarePipelineBuilder<RabbitMqSendMessageContext>(new NullBenzeneServiceContainer())
            .UseRabbitMqClient(PublishingChannel().Object)
            .Build();

        var client = new RabbitMqBenzeneMessageClient(pipeline,
            NullLogger<RabbitMqBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<string, string>("some-topic", "some-message");

        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }
}
