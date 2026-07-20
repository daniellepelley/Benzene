using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Core.Middleware;
using Benzene.Kafka.Core;
using Benzene.Kafka.Core.KafkaMessage;
using Benzene.Microsoft.Dependencies;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Kafka;

/// <summary>
/// Covers the Kafka worker's retry-then-dead-letter path (#29.1b) and the rebalance-drain wiring
/// (#29.1a): a persistently failing record is retried and routed to the dead-letter topic with
/// diagnostic headers, and <c>DrainOnRevoke</c> controls whether the consumer is built with the
/// partitions-revoked handler.
/// </summary>
public class KafkaWorkerDeadLetterAndDrainTest
{
    private static BenzeneKafkaConfig Config(bool commitOnlyOnSuccess = false, bool? drainOnRevoke = null) => new()
    {
        ConsumerConfig = new ConsumerConfig { GroupId = "test-group", BootstrapServers = "localhost:9092" },
        Topics = new[] { "orders" },
        CommitOnlyOnSuccess = commitOnlyOnSuccess,
        CatchHandlerExceptions = !commitOnlyOnSuccess,
        DrainOnRevoke = drainOnRevoke,
    };

    private static Mock<IConsumer<string, string>> ConsumerYielding(ConsumeResult<string, string> record)
    {
        var mockConsumer = new Mock<IConsumer<string, string>>();
        var served = 0;
        mockConsumer.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Returns(() => Interlocked.Increment(ref served) == 1 ? record : throw new OperationCanceledException());
        return mockConsumer;
    }

    [Fact]
    public void ShouldDrainOnRevoke_DefaultsToCommitOnlyOnSuccess()
    {
        Assert.False(Config(commitOnlyOnSuccess: false).ShouldDrainOnRevoke);
        Assert.True(Config(commitOnlyOnSuccess: true).ShouldDrainOnRevoke);
        Assert.False(Config(commitOnlyOnSuccess: true, drainOnRevoke: false).ShouldDrainOnRevoke);
        Assert.True(Config(commitOnlyOnSuccess: false, drainOnRevoke: true).ShouldDrainOnRevoke);
    }

    [Fact]
    public async Task DrainOnRevokeOff_BuildsConsumerWithoutARebalanceHandler()
    {
        var mockConsumer = ConsumerYielding(null!);
        // No record served (null would NRE the pipeline) - end immediately.
        mockConsumer.Setup(x => x.Consume(It.IsAny<CancellationToken>())).Throws(new OperationCanceledException());

        var mockFactory = new Mock<IKafkaConsumerFactory<string, string>>();
        mockFactory.Setup(x => x.Create(It.IsAny<ConsumerConfig>())).Returns(mockConsumer.Object);

        using var worker = new BenzeneKafkaWorker<string, string>(Mock.Of<Benzene.Abstractions.DI.IServiceResolverFactory>(),
            new KafkaApplication<string, string>(Mock.Of<Benzene.Abstractions.Middleware.IMiddlewarePipeline<KafkaRecordContext<string, string>>>()),
            Config(drainOnRevoke: false), Mock.Of<ILogger<BenzeneKafkaWorker<string, string>>>(), mockFactory.Object);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        mockFactory.Verify(x => x.Create(It.IsAny<ConsumerConfig>()), Times.Once);
        mockFactory.Verify(x => x.Create(It.IsAny<ConsumerConfig>(), It.IsAny<Action<ConsumerBuilder<string, string>>?>()), Times.Never);
    }

    [Fact]
    public async Task DrainOnRevokeOn_BuildsConsumerWithARebalanceHandler()
    {
        var mockConsumer = new Mock<IConsumer<string, string>>();
        mockConsumer.Setup(x => x.Consume(It.IsAny<CancellationToken>())).Throws(new OperationCanceledException());

        Action<ConsumerBuilder<string, string>>? capturedConfigure = null;
        var mockFactory = new Mock<IKafkaConsumerFactory<string, string>>();
        mockFactory.Setup(x => x.Create(It.IsAny<ConsumerConfig>(), It.IsAny<Action<ConsumerBuilder<string, string>>?>()))
            .Callback<ConsumerConfig, Action<ConsumerBuilder<string, string>>?>((_, configure) => capturedConfigure = configure)
            .Returns(mockConsumer.Object);

        using var worker = new BenzeneKafkaWorker<string, string>(Mock.Of<Benzene.Abstractions.DI.IServiceResolverFactory>(),
            new KafkaApplication<string, string>(Mock.Of<Benzene.Abstractions.Middleware.IMiddlewarePipeline<KafkaRecordContext<string, string>>>()),
            Config(drainOnRevoke: true), Mock.Of<ILogger<BenzeneKafkaWorker<string, string>>>(), mockFactory.Object);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        mockFactory.Verify(x => x.Create(It.IsAny<ConsumerConfig>(), It.IsAny<Action<ConsumerBuilder<string, string>>?>()), Times.Once);
        Assert.NotNull(capturedConfigure); // the worker supplied a rebalance-handler configuration step
    }

    [Fact]
    public async Task DeadLetter_RetriesThenProducesOriginalRecordWithDiagnosticHeaders()
    {
        var attempts = 0;
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<KafkaRecordContext<string, string>>(container);
        builder.Use((_, _) =>
        {
            Interlocked.Increment(ref attempts);
            throw new InvalidOperationException("boom");
        });
        var pipeline = builder.Build();
        using var resolverFactory = new MicrosoftServiceResolverFactory(services);
        var kafkaApplication = new KafkaApplication<string, string>(pipeline);

        var record = new ConsumeResult<string, string>
        {
            Message = new Message<string, string>
            {
                Key = "k",
                Value = "v",
                Headers = new Headers { { "orig-h", Encoding.UTF8.GetBytes("1") } },
            },
            TopicPartitionOffset = new TopicPartitionOffset("orders", new Partition(2), new Offset(42)),
        };
        var mockConsumer = ConsumerYielding(record);
        var mockFactory = new Mock<IKafkaConsumerFactory<string, string>>();
        mockFactory.Setup(x => x.Create(It.IsAny<ConsumerConfig>())).Returns(mockConsumer.Object);

        string producedTopic = null;
        Message<string, string> producedMessage = null;
        var produced = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var mockProducer = new Mock<IProducer<string, string>>();
        mockProducer.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((t, m, _) =>
            {
                producedTopic = t;
                producedMessage = m;
                produced.TrySetResult();
            })
            .ReturnsAsync(new DeliveryResult<string, string>());

        var deadLetter = new KafkaDeadLetterOptions<string, string>
        {
            DeadLetterTopic = "orders.DLT",
            MaxAttempts = 2,
            Producer = mockProducer.Object,
        };

        using var worker = new BenzeneKafkaWorker<string, string>(resolverFactory, kafkaApplication,
            Config(), Mock.Of<ILogger<BenzeneKafkaWorker<string, string>>>(), mockFactory.Object, deadLetter);

        await worker.StartAsync(CancellationToken.None);
        // Wait for the dead-letter produce (the record must be consumed and dispatched first), then stop.
        var completed = await Task.WhenAny(produced.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(produced.Task, completed);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(2, attempts); // retried up to MaxAttempts before dead-lettering
        mockProducer.Verify(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("orders.DLT", producedTopic);
        Assert.NotNull(producedMessage);
        Assert.Equal("k", producedMessage.Key);
        Assert.Equal("v", producedMessage.Value);

        string HeaderValue(string key) => Encoding.UTF8.GetString(GetHeaderBytes(producedMessage.Headers, key));
        Assert.Equal("InvalidOperationException", HeaderValue(KafkaDeadLetterOptions<string, string>.ReasonHeader));
        Assert.Equal("orders", HeaderValue(KafkaDeadLetterOptions<string, string>.OriginalTopicHeader));
        Assert.Equal("2", HeaderValue(KafkaDeadLetterOptions<string, string>.OriginalPartitionHeader));
        Assert.Equal("42", HeaderValue(KafkaDeadLetterOptions<string, string>.OriginalOffsetHeader));
        Assert.Equal("1", HeaderValue("orig-h")); // original headers preserved
    }

    private static byte[] GetHeaderBytes(Headers headers, string key)
    {
        Assert.True(headers.TryGetLastBytes(key, out var bytes), $"Expected header '{key}' on the dead-lettered message.");
        return bytes;
    }
}
