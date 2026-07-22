using System;
using System.Threading;
using System.Threading.Tasks;
using Benzene.HealthChecks.Core;
using Benzene.RabbitMq;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace Benzene.Test.RabbitMq;

public class RabbitMqHealthCheckTest
{
    private static Mock<IChannel> ChannelMock()
    {
        var channel = new Mock<IChannel>();
        channel.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return channel;
    }

    private static IRabbitMqConnectionProvider ProviderWith(Mock<IChannel> channel)
    {
        var connection = new Mock<IConnection>();
        connection.Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel.Object);
        var provider = new Mock<IRabbitMqConnectionProvider>();
        provider.Setup(x => x.GetConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection.Object);
        return provider.Object;
    }

    [Fact]
    public async Task ExecuteAsync_QueueExists_ReturnsHealthy_NonDestructively()
    {
        var channel = ChannelMock();
        channel.Setup(x => x.QueueDeclarePassiveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueueDeclareOk("orders", 0, 0));

        var check = new RabbitMqHealthCheck(ProviderWith(channel), "orders");
        var result = await check.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("RabbitMq", check.Type);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("Queue", dependency.Kind);
        Assert.Equal("orders", dependency.Name);
    }

    [Fact]
    public async Task ExecuteAsync_QueueMissing_ReturnsFailed_WithThe404ReplyCode()
    {
        var channel = ChannelMock();
        channel.Setup(x => x.QueueDeclarePassiveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Peer, 404, "NOT_FOUND")));

        var result = await new RabbitMqHealthCheck(ProviderWith(channel), "orders").ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal(404, result.Data["StatusCode"]);
        Assert.Equal("404", result.Data["ErrorCode"]);
        Assert.Equal("orders", Assert.Single(result.Dependencies).Name);
    }

    [Fact]
    public async Task ExecuteAsync_AccessRefused_DegradesToWarning()
    {
        var channel = ChannelMock();
        channel.Setup(x => x.QueueDeclarePassiveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Peer, 403, "ACCESS_REFUSED")));

        var result = await new RabbitMqHealthCheck(ProviderWith(channel), "orders").ExecuteAsync();

        // AMQP 403 access-refused is a permission problem: Warning, not a failure (§3.9).
        Assert.Equal(HealthCheckStatus.Warning, result.Status);
        Assert.Equal(403, result.Data["StatusCode"]);
    }

    [Fact]
    public async Task ExecuteAsync_BrokerUnreachable_ReturnsFailed()
    {
        var provider = new Mock<IRabbitMqConnectionProvider>();
        provider.Setup(x => x.GetConnectionAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("connection refused"));

        var result = await new RabbitMqHealthCheck(provider.Object, "orders").ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal("orders", Assert.Single(result.Dependencies).Name);
    }
}
