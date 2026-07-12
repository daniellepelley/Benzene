using System;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Sqs;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Sqs;

public class SqsApplicationExceptionLoggingTest
{
    [Fact]
    public async Task HandleAsync_PipelineThrows_LogsExceptionAndReportsBatchFailure()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<SqsMessageContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<SqsMessageContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var mockLogger = new Mock<IBenzeneLogger>();
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<IBenzeneLogger>()).Returns(mockLogger.Object);

        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);

        var application = new SqsApplication(mockPipeline.Object);

        var sqsEvent = new SQSEvent
        {
            Records =
            [
                new SQSEvent.SQSMessage { MessageId = "some-message-id" }
            ]
        };

        var response = await application.HandleAsync(sqsEvent, mockResolverFactory.Object);

        Assert.Single(response.BatchItemFailures);
        Assert.Equal("some-message-id", response.BatchItemFailures[0].ItemIdentifier);
        mockLogger.Verify(x => x.Log(
            BenzeneLogLevel.Error,
            It.Is<Exception>(ex => ex.Message == "boom"),
            It.IsAny<string>(),
            It.IsAny<object[]>()));
    }
}
