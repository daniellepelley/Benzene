using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.S3Events;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.S3;
using Benzene.Core.MessageHandlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.S3;

public class S3FailureHandlingTest
{
    private static S3Event CreateEvent(string key = "object-1")
    {
        return new S3Event
        {
            Records = new List<S3Event.S3EventNotificationRecord>
            {
                new S3Event.S3EventNotificationRecord
                {
                    EventSource = "aws:s3",
                    S3 = new S3Event.S3Entity { Object = new S3Event.S3ObjectEntity { Key = key } }
                }
            }
        };
    }

    private static Mock<IServiceResolverFactory> CreateResolverFactory()
    {
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<ILogger<S3Application>>()).Returns(Mock.Of<ILogger<S3Application>>());
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);
        return mockResolverFactory;
    }

    [Fact]
    public void S3Options_Defaults_AreCascadeAndDoNotEscalate()
    {
        var options = new S3Options();
        Assert.False(options.CatchExceptions);
        Assert.False(options.RaiseOnFailureStatus);
        Assert.Null(options.MaxDegreeOfParallelism);
    }

    [Fact]
    public async Task HandleAsync_DefaultOptions_HandlerThrows_ExceptionCascades()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<S3RecordContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<S3RecordContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var application = new S3Application(mockPipeline.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => application.HandleAsync(CreateEvent(), CreateResolverFactory().Object));
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusTrue_HandlerReturnsFailureResult_ThrowsS3MessageProcessingException()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<S3RecordContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<S3RecordContext>(), It.IsAny<IServiceResolver>()))
            .Callback<S3RecordContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var application = new S3Application(mockPipeline.Object, new S3Options { RaiseOnFailureStatus = true });

        var exception = await Assert.ThrowsAsync<S3MessageProcessingException>(
            () => application.HandleAsync(CreateEvent("object-2"), CreateResolverFactory().Object));
        Assert.Equal("object-2", exception.ObjectKey);
    }

    [Fact]
    public async Task HandleAsync_DefaultOptions_HandlerReturnsFailureResult_DoesNotThrow()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<S3RecordContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<S3RecordContext>(), It.IsAny<IServiceResolver>()))
            .Callback<S3RecordContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var application = new S3Application(mockPipeline.Object);

        // Default: a failure result is not escalated (fire-and-forget), so this completes.
        await application.HandleAsync(CreateEvent(), CreateResolverFactory().Object);
    }
}
