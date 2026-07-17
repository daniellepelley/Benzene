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

public class BenzeneKafkaWorkerTest
{
    private static BenzeneKafkaWorker<string, string> CreateWorker(BenzeneKafkaConfig config)
    {
        var pipeline = Mock.Of<IMiddlewarePipeline<KafkaRecordContext<string, string>>>();
        var kafkaApplication = new KafkaApplication<string, string>(pipeline);
        var resolverFactory = Mock.Of<IServiceResolverFactory>();
        var logger = Mock.Of<ILogger<BenzeneKafkaWorker<string, string>>>();

        return new BenzeneKafkaWorker<string, string>(resolverFactory, kafkaApplication, config, logger);
    }

    private static BenzeneKafkaConfig CreateConfig(bool commitOnlyOnSuccess, bool catchHandlerExceptions, bool preserveOrderPerPartition)
    {
        return new BenzeneKafkaConfig
        {
            ConsumerConfig = new ConsumerConfig { GroupId = "test-group", BootstrapServers = "localhost:9092" },
            Topics = new[] { "some-topic" },
            CommitOnlyOnSuccess = commitOnlyOnSuccess,
            CatchHandlerExceptions = catchHandlerExceptions,
            PreserveOrderPerPartition = preserveOrderPerPartition
        };
    }

    [Fact]
    public void CommitOnlyOnSuccess_DefaultsToFalse()
    {
        var config = new BenzeneKafkaConfig { ConsumerConfig = new ConsumerConfig(), Topics = new[] { "some-topic" } };

        Assert.False(config.CommitOnlyOnSuccess);
    }

    [Fact]
    public async Task StartAsync_CommitOnlyOnSuccessWithCatchHandlerExceptions_Throws()
    {
        var config = CreateConfig(commitOnlyOnSuccess: true, catchHandlerExceptions: true, preserveOrderPerPartition: true);
        var worker = CreateWorker(config);

        await Assert.ThrowsAsync<InvalidOperationException>(() => worker.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_CommitOnlyOnSuccessWithoutPreserveOrderPerPartition_Throws()
    {
        var config = CreateConfig(commitOnlyOnSuccess: true, catchHandlerExceptions: false, preserveOrderPerPartition: false);
        var worker = CreateWorker(config);

        await Assert.ThrowsAsync<InvalidOperationException>(() => worker.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_CommitOnlyOnSuccessWithValidCombination_SetsEnableAutoOffsetStoreFalse()
    {
        var config = CreateConfig(commitOnlyOnSuccess: true, catchHandlerExceptions: false, preserveOrderPerPartition: true);
        using var worker = CreateWorker(config);

        // EnableAutoOffsetStore is set synchronously before the background consume loop is started,
        // so it's already observable the instant StartAsync returns - no need to wait for the loop.
        await worker.StartAsync(CancellationToken.None);

        Assert.False(config.ConsumerConfig.EnableAutoOffsetStore);

        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_CommitOnlyOnSuccessFalse_DoesNotTouchEnableAutoOffsetStore()
    {
        var config = CreateConfig(commitOnlyOnSuccess: false, catchHandlerExceptions: true, preserveOrderPerPartition: true);
        using var worker = CreateWorker(config);

        await worker.StartAsync(CancellationToken.None);

        Assert.Null(config.ConsumerConfig.EnableAutoOffsetStore);

        await worker.StopAsync(CancellationToken.None);
    }
}
