using System;
using System.Collections.Generic;
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
    public void SqsConsumerOptions_DefaultAckMode_IsWholeBatch()
    {
        Assert.Equal(SqsConsumerAckMode.WholeBatch, new SqsConsumerOptions().AckMode);
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
    public async Task HandleAsync_DefaultWholeBatch_OneMessageThrows_PropagatesException()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<SqsConsumerMessageContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.Is<SqsConsumerMessageContext>(c => c.Message.MessageId == "fails"), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        mockPipeline.Setup(x => x.HandleAsync(It.Is<SqsConsumerMessageContext>(c => c.Message.MessageId == "succeeds"), It.IsAny<IServiceResolver>()))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new SqsConsumerApplication(mockPipeline.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => application.HandleAsync(CreateTwoMessageResponse(), resolverFactory.Object));
    }

    [Fact]
    public async Task HandleAsync_PerMessage_OneMessageThrows_ReturnsSplitResultWithoutThrowing()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<SqsConsumerMessageContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.Is<SqsConsumerMessageContext>(c => c.Message.MessageId == "fails"), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        mockPipeline.Setup(x => x.HandleAsync(It.Is<SqsConsumerMessageContext>(c => c.Message.MessageId == "succeeds"), It.IsAny<IServiceResolver>()))
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
            .Callback<SqsConsumerMessageContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);
        mockPipeline.Setup(x => x.HandleAsync(It.Is<SqsConsumerMessageContext>(c => c.Message.MessageId == "succeeds"), It.IsAny<IServiceResolver>()))
            .Callback<SqsConsumerMessageContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(true))
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
