using System;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Kafka.Core;
using Benzene.Kafka.Core.KafkaMessage;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Kafka;

public class KafkaConsumerFactoryTest
{
    private static BenzeneKafkaWorker<string, string> CreateWorker(BenzeneKafkaConfig config,
        IKafkaConsumerFactory<string, string> consumerFactory)
    {
        var pipeline = Mock.Of<IMiddlewarePipeline<KafkaRecordContext<string, string>>>();
        var kafkaApplication = new KafkaApplication<string, string>(pipeline);
        var resolverFactory = Mock.Of<IServiceResolverFactory>();
        var logger = Mock.Of<ILogger<BenzeneKafkaWorker<string, string>>>();

        return new BenzeneKafkaWorker<string, string>(resolverFactory, kafkaApplication, config, logger, consumerFactory);
    }

    private static BenzeneKafkaConfig CreateConfig()
    {
        return new BenzeneKafkaConfig
        {
            ConsumerConfig = new ConsumerConfig { GroupId = "test-group", BootstrapServers = "localhost:9092" },
            Topics = new[] { "some-topic" }
        };
    }

    private static Mock<IConsumer<string, string>> CreateIdleConsumer()
    {
        var mockConsumer = new Mock<IConsumer<string, string>>();
        // End the consume loop cleanly the moment it starts - the loop treats
        // OperationCanceledException as an ordinary shutdown signal.
        mockConsumer.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Throws(new OperationCanceledException());
        return mockConsumer;
    }

    [Fact]
    public async Task Worker_CreatesTheConsumerThroughTheFactory_AndSubscribesIt()
    {
        var config = CreateConfig();
        var mockConsumer = CreateIdleConsumer();
        var mockFactory = new Mock<IKafkaConsumerFactory<string, string>>();
        mockFactory.Setup(x => x.Create(It.IsAny<ConsumerConfig>())).Returns(mockConsumer.Object);

        using var worker = CreateWorker(config, mockFactory.Object);
        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        // The factory receives the worker's own ConsumerConfig instance, so any worker-applied
        // adjustments are visible to a custom factory building from it.
        mockFactory.Verify(x => x.Create(config.ConsumerConfig), Times.Once);
        mockConsumer.Verify(x => x.Subscribe(config.Topics), Times.Once);
        mockConsumer.Verify(x => x.Close(), Times.Once);
        mockConsumer.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task CommitOnlyOnSuccess_AdjustsTheConfig_BeforeTheFactorySeesIt()
    {
        var config = CreateConfig();
        config.CommitOnlyOnSuccess = true;
        config.CatchHandlerExceptions = false;

        bool? autoOffsetStoreAtCreation = null;
        var mockConsumer = CreateIdleConsumer();
        var mockFactory = new Mock<IKafkaConsumerFactory<string, string>>();
        mockFactory.Setup(x => x.Create(It.IsAny<ConsumerConfig>()))
            .Callback<ConsumerConfig>(consumerConfig => autoOffsetStoreAtCreation = consumerConfig.EnableAutoOffsetStore)
            .Returns(mockConsumer.Object);

        using var worker = CreateWorker(config, mockFactory.Object);
        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.False(autoOffsetStoreAtCreation);
    }

    [Fact]
    public void DefaultFactory_AppliesTheConfigureAction()
    {
        var configured = false;
        var factory = new KafkaConsumerFactory<string, string>(builder =>
        {
            configured = true;
            Assert.NotNull(builder);
        });

        using var consumer = factory.Create(new ConsumerConfig { GroupId = "test-group", BootstrapServers = "localhost:9092" });

        Assert.True(configured);
        Assert.NotNull(consumer);
    }
}
