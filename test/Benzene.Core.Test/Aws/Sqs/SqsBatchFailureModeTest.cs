using System;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Sqs;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Sqs;

public class SqsBatchFailureModeTest
{
    [Fact]
    public void SqsOptions_DefaultBatchFailureMode_IsPartialBatchFailure()
    {
        Assert.Equal(SqsBatchFailureMode.PartialBatchFailure, new SqsOptions().BatchFailureMode);
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
        mockResolver.Setup(x => x.GetService<Microsoft.Extensions.Logging.ILogger<SqsApplication>>())
            .Returns(Mock.Of<Microsoft.Extensions.Logging.ILogger<SqsApplication>>());

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
