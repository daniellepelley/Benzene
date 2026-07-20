using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Sqs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Sqs;

public class SqsBatchFailureModeTest
{
    [Fact]
    public async Task HandleAsync_ManyRecordsFailConcurrently_ReportsExactlyTheFailedIds()
    {
        // Partial-batch-failure reporting is built from tasks running concurrently under Task.WhenAll.
        // A yielding pipeline forces the failure continuations to resume on pool threads at the same
        // time - the exact condition under which a shared, non-thread-safe List<>.Add would drop or
        // duplicate a failed id (silent SQS message loss) or throw mid-resize. Assert the reported set
        // is EXACTLY the failed set, over repeated runs.
        var mockPipeline = new Mock<IMiddlewarePipeline<SqsMessageContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<SqsMessageContext>(), It.IsAny<IServiceResolver>()))
            .Returns(async (SqsMessageContext context, IServiceResolver _) =>
            {
                await Task.Yield();
                if (context.SqsMessage.MessageId.StartsWith("fail"))
                {
                    throw new InvalidOperationException("boom");
                }
            });

        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<ILogger<SqsApplication>>()).Returns(Mock.Of<ILogger<SqsApplication>>());

        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);

        var application = new SqsApplication(mockPipeline.Object);

        var records = Enumerable.Range(0, 60)
            .Select(i => new SQSEvent.SQSMessage { MessageId = i % 2 == 0 ? $"ok-{i}" : $"fail-{i}" })
            .ToList();
        var expectedFailures = records.Select(r => r.MessageId).Where(id => id.StartsWith("fail")).OrderBy(id => id).ToArray();

        for (var run = 0; run < 10; run++)
        {
            var response = await application.HandleAsync(new SQSEvent { Records = records }, mockResolverFactory.Object);

            var reported = response.BatchItemFailures.Select(f => f.ItemIdentifier).OrderBy(id => id).ToArray();
            Assert.Equal(expectedFailures, reported);
        }
    }

    [Fact]
    public void SqsOptions_DefaultBatchFailureMode_IsPartialBatchFailure()
    {
        Assert.Equal(SqsBatchFailureMode.PartialBatchFailure, new SqsOptions().BatchFailureMode);
    }

    [Fact]
    public void SqsOptions_DefaultMaxDegreeOfParallelism_IsNull()
    {
        Assert.Null(new SqsOptions().MaxDegreeOfParallelism);
    }

    [Fact]
    public async Task HandleAsync_WithMaxDegreeOfParallelism_NeverRunsMoreThanThatManyRecordsAtOnce()
    {
        var gate = new object();
        var current = 0;
        var maxObserved = 0;

        var mockPipeline = new Mock<IMiddlewarePipeline<SqsMessageContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<SqsMessageContext>(), It.IsAny<IServiceResolver>()))
            .Returns(async (SqsMessageContext _, IServiceResolver __) =>
            {
                lock (gate)
                {
                    current++;
                    maxObserved = Math.Max(maxObserved, current);
                }

                await Task.Delay(50);

                lock (gate)
                {
                    current--;
                }
            });

        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);

        var application = new SqsApplication(mockPipeline.Object, new SqsOptions { MaxDegreeOfParallelism = 3 });

        var records = Enumerable.Range(0, 30).Select(i => new SQSEvent.SQSMessage { MessageId = $"ok-{i}" }).ToList();

        await application.HandleAsync(new SQSEvent { Records = records }, mockResolverFactory.Object);

        Assert.True(maxObserved <= 3, $"Expected at most 3 records concurrently, observed {maxObserved}.");
        Assert.Equal(3, maxObserved);
    }

    [Fact]
    public async Task HandleAsync_FailWholeBatch_OneMessageFails_ThrowsSqsBatchProcessingExceptionListingFailedIds()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<SqsMessageContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.Is<SqsMessageContext>(c => c.SqsMessage.MessageId == "fails"), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        mockPipeline
            .Setup(x => x.HandleAsync(It.Is<SqsMessageContext>(c => c.SqsMessage.MessageId == "succeeds"), It.IsAny<IServiceResolver>()))
            .Returns(Task.CompletedTask);

        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<ILogger<SqsApplication>>())
            .Returns(Mock.Of<ILogger<SqsApplication>>());

        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);

        var application = new SqsApplication(mockPipeline.Object, new SqsOptions { BatchFailureMode = SqsBatchFailureMode.FailWholeBatch });

        var sqsEvent = new SQSEvent
        {
            Records =
            [
                new SQSEvent.SQSMessage { MessageId = "succeeds" },
                new SQSEvent.SQSMessage { MessageId = "fails" }
            ]
        };

        var exception = await Assert.ThrowsAsync<SqsBatchProcessingException>(() => application.HandleAsync(sqsEvent, mockResolverFactory.Object));
        Assert.Equal(["fails"], exception.FailedMessageIds);
    }

    [Fact]
    public async Task HandleAsync_FailWholeBatch_AllMessagesSucceed_ReturnsEmptyBatchResponseWithoutThrowing()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<SqsMessageContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<SqsMessageContext>(), It.IsAny<IServiceResolver>()))
            .Returns(Task.CompletedTask);

        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());

        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);

        var application = new SqsApplication(mockPipeline.Object, new SqsOptions { BatchFailureMode = SqsBatchFailureMode.FailWholeBatch });

        var sqsEvent = new SQSEvent
        {
            Records = [new SQSEvent.SQSMessage { MessageId = "ok" }]
        };

        var response = await application.HandleAsync(sqsEvent, mockResolverFactory.Object);

        Assert.Empty(response.BatchItemFailures);
    }
}
