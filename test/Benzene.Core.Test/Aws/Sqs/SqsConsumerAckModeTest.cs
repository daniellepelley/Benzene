using System;
using Benzene.Results;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Core.MessageHandlers;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Sqs;

public class SqsConsumerAckModeTest
{
    [Fact]
    public async Task HandleAsync_ManyMessagesFailConcurrently_ReportsExactlyTheFailedMessages()
    {
        // The per-message outcomes are collected from tasks running concurrently under Task.WhenAll.
        // A yielding pipeline forces the continuations to resume on pool threads at the same time -
        // the exact condition under which a shared, non-thread-safe List<>.Add would drop a failed
        // message (which then lands in SuccessfulMessages and gets deleted from the queue) or throw.
        var mockPipeline = new Mock<IMiddlewarePipeline<SqsConsumerMessageContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<SqsConsumerMessageContext>(), It.IsAny<IServiceResolver>()))
            .Returns(async (SqsConsumerMessageContext context, IServiceResolver _) =>
            {
                await Task.Yield();
                context.MessageResult = (!context.Message.MessageId.StartsWith("fail") ? BenzeneResult.Ok() : BenzeneResult.UnexpectedError());
            });

        var (_, resolverFactory) = CreateResolver();
        var application = new SqsConsumerApplication(mockPipeline.Object, new SqsConsumerOptions { AckMode = SqsConsumerAckMode.PerMessage });

        var messages = Enumerable.Range(0, 60)
            .Select(i => new Message { MessageId = i % 2 == 0 ? $"ok-{i}" : $"fail-{i}", ReceiptHandle = $"r{i}" })
            .ToList();
        var response = new ReceiveMessageResponse { Messages = messages };
        var expectedFailed = messages.Select(m => m.MessageId).Where(id => id.StartsWith("fail")).OrderBy(id => id).ToArray();

        for (var run = 0; run < 10; run++)
        {
            var result = await application.HandleAsync(response, resolverFactory.Object);

            var reportedFailed = result.FailedMessages.Select(m => m.MessageId).OrderBy(id => id).ToArray();
            Assert.Equal(expectedFailed, reportedFailed);
            Assert.Equal(30, result.SuccessfulMessages.Count);
        }
    }

    [Fact]
    public void SqsConsumerOptions_DefaultAckMode_IsPerMessage()
    {
        Assert.Equal(SqsConsumerAckMode.PerMessage, new SqsConsumerOptions().AckMode);
    }

    [Fact]
    public async Task HandleAsync_MessageOutcomeNeverSet_ExcludedFromSuccessfulMessages_NotSilentlyDeleted()
    {
        // An unrouted message whose result setter never ran (null MessageResult) must NOT be counted
        // as successful (which would delete it) - it stays for redelivery/DLQ redrive.
        var mockPipeline = new Mock<IMiddlewarePipeline<SqsConsumerMessageContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<SqsConsumerMessageContext>(), It.IsAny<IServiceResolver>()))
            .Returns(Task.CompletedTask); // never sets context.MessageResult

        var (_, resolverFactory) = CreateResolver();
        var application = new SqsConsumerApplication(mockPipeline.Object, new SqsConsumerOptions { AckMode = SqsConsumerAckMode.PerMessage });

        var result = await application.HandleAsync(
            new ReceiveMessageResponse { Messages = new List<Message> { new Message { MessageId = "unrouted", ReceiptHandle = "r1" } } },
            resolverFactory.Object);

        Assert.Empty(result.SuccessfulMessages);
        Assert.Equal("unrouted", Assert.Single(result.FailedMessages).MessageId);
    }

    private static (Mock<IServiceResolver> Resolver, Mock<IServiceResolverFactory> ResolverFactory) CreateResolver()
    {
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);
        return (mockResolver, mockResolverFactory);
    }

    private static ReceiveMessageResponse CreateTwoMessageResponse()
    {
        return new ReceiveMessageResponse
        {
            Messages = new List<Message>
            {
                new Message { MessageId = "succeeds", ReceiptHandle = "r1" },
                new Message { MessageId = "fails", ReceiptHandle = "r2" }
            }
        };
    }

    [Fact]
    public async Task HandleAsync_WholeBatch_OneMessageThrows_PropagatesException()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<SqsConsumerMessageContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.Is<SqsConsumerMessageContext>(c => c.Message.MessageId == "fails"), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        mockPipeline.Setup(x => x.HandleAsync(It.Is<SqsConsumerMessageContext>(c => c.Message.MessageId == "succeeds"), It.IsAny<IServiceResolver>()))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new SqsConsumerApplication(mockPipeline.Object, new SqsConsumerOptions { AckMode = SqsConsumerAckMode.WholeBatch });

        await Assert.ThrowsAsync<InvalidOperationException>(() => application.HandleAsync(CreateTwoMessageResponse(), resolverFactory.Object));
    }

    [Fact]
    public async Task HandleAsync_PerMessage_OneMessageThrows_ReturnsSplitResultWithoutThrowing()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<SqsConsumerMessageContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.Is<SqsConsumerMessageContext>(c => c.Message.MessageId == "fails"), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        mockPipeline.Setup(x => x.HandleAsync(It.Is<SqsConsumerMessageContext>(c => c.Message.MessageId == "succeeds"), It.IsAny<IServiceResolver>()))
            .Callback<SqsConsumerMessageContext, IServiceResolver>((context, _) => context.MessageResult = BenzeneResult.Ok())
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new SqsConsumerApplication(mockPipeline.Object, new SqsConsumerOptions { AckMode = SqsConsumerAckMode.PerMessage });

        var result = await application.HandleAsync(CreateTwoMessageResponse(), resolverFactory.Object);

        Assert.Single(result.SuccessfulMessages);
        Assert.Equal("succeeds", result.SuccessfulMessages[0].MessageId);
        Assert.Single(result.FailedMessages);
        Assert.Equal("fails", result.FailedMessages[0].MessageId);
    }

    [Fact]
    public async Task HandleAsync_PerMessage_HandlerReturnsFailureResult_ExcludedFromSuccessfulMessages()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<SqsConsumerMessageContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.Is<SqsConsumerMessageContext>(c => c.Message.MessageId == "fails"), It.IsAny<IServiceResolver>()))
            .Callback<SqsConsumerMessageContext, IServiceResolver>((context, _) => context.MessageResult = BenzeneResult.UnexpectedError())
            .Returns(Task.CompletedTask);
        mockPipeline.Setup(x => x.HandleAsync(It.Is<SqsConsumerMessageContext>(c => c.Message.MessageId == "succeeds"), It.IsAny<IServiceResolver>()))
            .Callback<SqsConsumerMessageContext, IServiceResolver>((context, _) => context.MessageResult = BenzeneResult.Ok())
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new SqsConsumerApplication(mockPipeline.Object, new SqsConsumerOptions { AckMode = SqsConsumerAckMode.PerMessage });

        var result = await application.HandleAsync(CreateTwoMessageResponse(), resolverFactory.Object);

        Assert.Single(result.SuccessfulMessages);
        Assert.Equal("succeeds", result.SuccessfulMessages[0].MessageId);
        Assert.Single(result.FailedMessages);
        Assert.Equal("fails", result.FailedMessages[0].MessageId);
    }
}
