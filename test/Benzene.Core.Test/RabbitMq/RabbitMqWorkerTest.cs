using System;
using Benzene.Results;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.RabbitMq;
using Benzene.RabbitMq.RabbitMqMessage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace Benzene.Test.RabbitMq;

public class RabbitMqWorkerTest
{
    private sealed class Harness
    {
        public required RabbitMqWorker Worker { get; init; }
        public required Mock<IChannel> Channel { get; init; }
        public required Func<AsyncEventingBasicConsumer> GetConsumer { get; init; }
    }

    // Builds a worker over a fully mocked connection/channel. The pipeline records `result`
    // (or throws when `throws` is set) so we can assert the ack/nack the worker chooses.
    private static Harness CreateHarness(RabbitMqConfig config, bool? result, bool throws = false)
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<RabbitMqContext>>();
        var setup = mockPipeline.Setup(x => x.HandleAsync(It.IsAny<RabbitMqContext>(), It.IsAny<IServiceResolver>()));
        if (throws)
        {
            setup.ThrowsAsync(new InvalidOperationException("boom"));
        }
        else
        {
            setup.Callback<RabbitMqContext, IServiceResolver>((context, _) =>
            {
                if (result.HasValue)
                {
                    context.MessageResult = (result.Value ? BenzeneResult.Ok() : BenzeneResult.UnexpectedError());
                }
            }).Returns(Task.CompletedTask);
        }

        var application = new RabbitMqApplication(mockPipeline.Object);

        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);

        var mockChannel = new Mock<IChannel>();
        mockChannel.Setup(x => x.BasicQosAsync(It.IsAny<uint>(), It.IsAny<ushort>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        AsyncEventingBasicConsumer? captured = null;
        mockChannel.Setup(x => x.BasicConsumeAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object?>?>(),
                It.IsAny<IAsyncBasicConsumer>(), It.IsAny<CancellationToken>()))
            .Callback((string _, bool _, string _, bool _, bool _, IDictionary<string, object?>? _, IAsyncBasicConsumer consumer, CancellationToken _) =>
                captured = (AsyncEventingBasicConsumer)consumer)
            .ReturnsAsync("consumer-tag");

        var mockConnection = new Mock<IConnection>();
        mockConnection.Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockChannel.Object);

        var mockConnectionFactory = new Mock<IRabbitMqConnectionFactory>();
        mockConnectionFactory.Setup(x => x.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockConnection.Object);

        var worker = new RabbitMqWorker(mockResolverFactory.Object, application, config,
            mockConnectionFactory.Object, NullLogger<RabbitMqWorker>.Instance);

        return new Harness
        {
            Worker = worker,
            Channel = mockChannel,
            GetConsumer = () => captured ?? throw new InvalidOperationException("consumer was not captured - StartAsync not called?"),
        };
    }

    private static Task FireDeliveryAsync(AsyncEventingBasicConsumer consumer, ulong deliveryTag, bool redelivered)
    {
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?> { ["topic"] = Encoding.UTF8.GetBytes("orderCreated") },
        };
        return consumer.HandleBasicDeliverAsync("consumer-tag", deliveryTag, redelivered, "exchange",
            "orderCreated", properties, Encoding.UTF8.GetBytes("{}"));
    }

    // Awaits until `predicate` holds (the dispatched handler runs on a background lane task), or fails.
    private static async Task WaitForAsync(Func<bool> predicate)
    {
        for (var i = 0; i < 100 && !predicate(); i++)
        {
            await Task.Delay(20);
        }

        Assert.True(predicate(), "condition not met within timeout");
    }

    private static RabbitMqConfig Config(bool requeueOnFailure = true) => new()
    {
        QueueName = "queue",
        ConcurrentRequests = 1,
        RequeueOnFailure = requeueOnFailure,
    };

    [Fact]
    public async Task SuccessfulHandler_Acks()
    {
        var harness = CreateHarness(Config(), result: true);
        await harness.Worker.StartAsync(CancellationToken.None);

        await FireDeliveryAsync(harness.GetConsumer(), 7, redelivered: false);

        await WaitForAsync(() => harness.Channel.Invocations.Count > 0 &&
            harness.Channel.Invocations.Count(i => i.Method.Name == nameof(IChannel.BasicAckAsync)) > 0);
        harness.Channel.Verify(x => x.BasicAckAsync(7, false, It.IsAny<CancellationToken>()), Times.Once);
        harness.Channel.Verify(x => x.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FailureResult_FirstDelivery_NacksWithRequeue()
    {
        var harness = CreateHarness(Config(), result: false);
        await harness.Worker.StartAsync(CancellationToken.None);

        await FireDeliveryAsync(harness.GetConsumer(), 8, redelivered: false);

        await WaitForAsync(() => harness.Channel.Invocations.Count(i => i.Method.Name == nameof(IChannel.BasicNackAsync)) > 0);
        harness.Channel.Verify(x => x.BasicNackAsync(8, false, true, It.IsAny<CancellationToken>()), Times.Once);
        harness.Channel.Verify(x => x.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FailureResult_Redelivered_NacksWithoutRequeue_BoundsPoisonLoop()
    {
        var harness = CreateHarness(Config(), result: false);
        await harness.Worker.StartAsync(CancellationToken.None);

        await FireDeliveryAsync(harness.GetConsumer(), 9, redelivered: true);

        await WaitForAsync(() => harness.Channel.Invocations.Count(i => i.Method.Name == nameof(IChannel.BasicNackAsync)) > 0);
        harness.Channel.Verify(x => x.BasicNackAsync(9, false, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FailureResult_RequeueDisabled_NacksWithoutRequeue()
    {
        var harness = CreateHarness(Config(requeueOnFailure: false), result: false);
        await harness.Worker.StartAsync(CancellationToken.None);

        await FireDeliveryAsync(harness.GetConsumer(), 10, redelivered: false);

        await WaitForAsync(() => harness.Channel.Invocations.Count(i => i.Method.Name == nameof(IChannel.BasicNackAsync)) > 0);
        harness.Channel.Verify(x => x.BasicNackAsync(10, false, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandlerThrows_Nacks()
    {
        var harness = CreateHarness(Config(), result: null, throws: true);
        await harness.Worker.StartAsync(CancellationToken.None);

        await FireDeliveryAsync(harness.GetConsumer(), 11, redelivered: false);

        await WaitForAsync(() => harness.Channel.Invocations.Count(i => i.Method.Name == nameof(IChannel.BasicNackAsync)) > 0);
        harness.Channel.Verify(x => x.BasicNackAsync(11, false, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NoResultRecorded_Acks()
    {
        // Nothing set a MessageResult (result: null, no throw) - the worker treats "not unsuccessful"
        // as success and acks, so a pipeline that never records a result doesn't wedge the queue.
        var harness = CreateHarness(Config(), result: null);
        await harness.Worker.StartAsync(CancellationToken.None);

        await FireDeliveryAsync(harness.GetConsumer(), 12, redelivered: false);

        await WaitForAsync(() => harness.Channel.Invocations.Count(i => i.Method.Name == nameof(IChannel.BasicAckAsync)) > 0);
        harness.Channel.Verify(x => x.BasicAckAsync(12, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoAckMode_ConsumesWithAutoAck_AndDoesNotSettleManually()
    {
        var config = Config();
        config.AckMode = RabbitMqAckMode.AutoAck;
        var harness = CreateHarness(config, result: true);

        await harness.Worker.StartAsync(CancellationToken.None);

        // Consumed with autoAck: true.
        harness.Channel.Verify(x => x.BasicConsumeAsync("queue", true, It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<bool>(), It.IsAny<IDictionary<string, object?>?>(), It.IsAny<IAsyncBasicConsumer>(),
            It.IsAny<CancellationToken>()), Times.Once);

        await FireDeliveryAsync(harness.GetConsumer(), 13, redelivered: false);
        await Task.Delay(100);

        harness.Channel.Verify(x => x.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        harness.Channel.Verify(x => x.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Config_Defaults()
    {
        var config = new RabbitMqConfig { QueueName = "q" };

        Assert.Equal(RabbitMqAckMode.Explicit, config.AckMode);
        Assert.Equal((ushort)5, config.PrefetchCount);
        Assert.Equal(5, config.ConcurrentRequests);
        Assert.True(config.RequeueOnFailure);
    }
}
