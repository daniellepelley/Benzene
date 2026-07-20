using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.KafkaEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Kafka;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Kafka;

public class KafkaBatchFailureModeTest
{
    private static Mock<IServiceResolverFactory> MockResolverFactory()
    {
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<ILogger<KafkaApplication>>()).Returns(Mock.Of<ILogger<KafkaApplication>>());

        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);
        return mockResolverFactory;
    }

    private static KafkaEvent EventWith(params (string PartitionKey, KafkaEvent.KafkaEventRecord[] Records)[] partitions)
    {
        return new KafkaEvent
        {
            EventSource = "aws:kafka",
            Records = partitions.ToDictionary(p => p.PartitionKey, p => (IList<KafkaEvent.KafkaEventRecord>)p.Records.ToList())
        };
    }

    private static KafkaEvent.KafkaEventRecord Record(long offset) =>
        new() { Topic = "topic", Partition = 0, Offset = offset };

    [Fact]
    public void KafkaOptions_Defaults_ArePartialBatchFailureAndUnbounded()
    {
        Assert.Equal(KafkaBatchFailureMode.PartialBatchFailure, new KafkaOptions().BatchFailureMode);
        Assert.Null(new KafkaOptions().MaxDegreeOfParallelism);
    }

    [Fact]
    public async Task HandleAsync_RecordsWithinAPartition_ProcessInOffsetOrder()
    {
        var processedOffsets = new List<long>();

        var mockPipeline = new Mock<IMiddlewarePipeline<KafkaContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<KafkaContext>(), It.IsAny<IServiceResolver>()))
            .Returns((KafkaContext context, IServiceResolver _) =>
            {
                processedOffsets.Add(context.KafkaEventRecord.Offset);
                context.MessageResult = Mock.Of<IMessageResult>(r => r.IsSuccessful == true);
                return Task.CompletedTask;
            });

        var application = new KafkaApplication(mockPipeline.Object);

        // Records handed to us out of offset order within the one partition.
        var @event = EventWith(("topic-0", new[] { Record(2), Record(0), Record(1) }));

        await application.HandleAsync(@event, MockResolverFactory().Object);

        Assert.Equal(new long[] { 0, 1, 2 }, processedOffsets);
    }

    [Fact]
    public async Task HandleAsync_FailureInPartition_ReportsFirstFailedOffset_AndStopsThatPartition()
    {
        var processedOffsets = new List<long>();

        var mockPipeline = new Mock<IMiddlewarePipeline<KafkaContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<KafkaContext>(), It.IsAny<IServiceResolver>()))
            .Returns((KafkaContext context, IServiceResolver _) =>
            {
                processedOffsets.Add(context.KafkaEventRecord.Offset);
                // Offset 1 in the failing partition returns an unsuccessful result.
                var succeeded = !(context.KafkaEventRecord.Topic == "fail" && context.KafkaEventRecord.Offset == 1);
                context.MessageResult = Mock.Of<IMessageResult>(r => r.IsSuccessful == succeeded);
                return Task.CompletedTask;
            });

        var application = new KafkaApplication(mockPipeline.Object);

        var failing = new[]
        {
            new KafkaEvent.KafkaEventRecord { Topic = "fail", Offset = 0 },
            new KafkaEvent.KafkaEventRecord { Topic = "fail", Offset = 1 },
            new KafkaEvent.KafkaEventRecord { Topic = "fail", Offset = 2 }
        };
        var healthy = new[]
        {
            new KafkaEvent.KafkaEventRecord { Topic = "ok", Offset = 5 },
            new KafkaEvent.KafkaEventRecord { Topic = "ok", Offset = 6 }
        };

        var response = await application.HandleAsync(
            EventWith(("my-topic-3", failing), ("my-topic-9", healthy)), MockResolverFactory().Object);

        // Exactly one partition failed, reported at the first failed offset.
        var failure = Assert.Single(response.BatchItemFailures);
        Assert.Equal("my-topic-3", failure.ItemIdentifier.Partition);
        Assert.Equal(1, failure.ItemIdentifier.Offset);

        // The failing partition stopped at offset 1 — offset 2 was never processed.
        Assert.DoesNotContain(2L, processedOffsets.Where(o => o >= 2 && o < 5));
        // The healthy partition ran to completion.
        Assert.Contains(5L, processedOffsets);
        Assert.Contains(6L, processedOffsets);
    }

    [Fact]
    public async Task HandleAsync_ThrownException_ContainsToThatPartition_AndReportsIt()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<KafkaContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<KafkaContext>(), It.IsAny<IServiceResolver>()))
            .Returns((KafkaContext context, IServiceResolver _) =>
            {
                if (context.KafkaEventRecord.Offset == 0)
                {
                    throw new InvalidOperationException("boom");
                }

                context.MessageResult = Mock.Of<IMessageResult>(r => r.IsSuccessful == true);
                return Task.CompletedTask;
            });

        var application = new KafkaApplication(mockPipeline.Object);

        var response = await application.HandleAsync(
            EventWith(("throws-0", new[] { Record(0) }), ("ok-1", new[] { Record(7) })),
            MockResolverFactory().Object);

        var failure = Assert.Single(response.BatchItemFailures);
        Assert.Equal("throws-0", failure.ItemIdentifier.Partition);
        Assert.Equal(0, failure.ItemIdentifier.Offset);
    }

    [Fact]
    public async Task HandleAsync_UnsetOutcome_IsSkipped_NotReportedAsFailure()
    {
        // An unroutable record (topic matched no handler, so the result setter never ran) leaves
        // MessageResult null. Kafka resume-on-failure has no per-record DLQ, so an unset outcome must
        // not wedge the partition into an infinite resume loop — it's treated as processed.
        var mockPipeline = new Mock<IMiddlewarePipeline<KafkaContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<KafkaContext>(), It.IsAny<IServiceResolver>()))
            .Returns(Task.CompletedTask); // never sets MessageResult

        var application = new KafkaApplication(mockPipeline.Object);

        var response = await application.HandleAsync(
            EventWith(("topic-0", new[] { Record(0), Record(1) })), MockResolverFactory().Object);

        Assert.Empty(response.BatchItemFailures);
    }

    [Fact]
    public async Task HandleAsync_FailWholeBatch_AnyFailure_ThrowsListingFailedPartitions()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<KafkaContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<KafkaContext>(), It.IsAny<IServiceResolver>()))
            .Returns((KafkaContext context, IServiceResolver _) =>
            {
                var succeeded = context.KafkaEventRecord.Topic != "fail";
                context.MessageResult = Mock.Of<IMessageResult>(r => r.IsSuccessful == succeeded);
                return Task.CompletedTask;
            });

        var application = new KafkaApplication(mockPipeline.Object, new KafkaOptions { BatchFailureMode = KafkaBatchFailureMode.FailWholeBatch });

        var @event = EventWith(
            ("bad-2", new[] { new KafkaEvent.KafkaEventRecord { Topic = "fail", Offset = 0 } }),
            ("good-4", new[] { new KafkaEvent.KafkaEventRecord { Topic = "ok", Offset = 0 } }));

        var exception = await Assert.ThrowsAsync<KafkaBatchProcessingException>(() =>
            application.HandleAsync(@event, MockResolverFactory().Object));

        Assert.Equal(new[] { "bad-2" }, exception.FailedPartitions);
    }

    [Fact]
    public async Task HandleAsync_AllSucceed_ReturnsEmptyBatchResponse()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<KafkaContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<KafkaContext>(), It.IsAny<IServiceResolver>()))
            .Returns((KafkaContext context, IServiceResolver _) =>
            {
                context.MessageResult = Mock.Of<IMessageResult>(r => r.IsSuccessful == true);
                return Task.CompletedTask;
            });

        var application = new KafkaApplication(mockPipeline.Object);

        var response = await application.HandleAsync(
            EventWith(("topic-0", new[] { Record(0), Record(1) })), MockResolverFactory().Object);

        Assert.Empty(response.BatchItemFailures);
    }
}
