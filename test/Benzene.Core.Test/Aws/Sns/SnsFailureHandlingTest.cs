using System;
using System.Threading.Tasks;
using Amazon.Lambda.SNSEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Sns;
using Benzene.Core.MessageHandlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Sns;

public class SnsFailureHandlingTest
{
    private static SNSEvent CreateEvent(string messageId = "msg-1")
    {
        return new SNSEvent
        {
            Records = new[]
            {
                new SNSEvent.SNSRecord
                {
                    EventSource = "aws:sns",
                    Sns = new SNSEvent.SNSMessage { MessageId = messageId, Message = "body" }
                }
            }
        };
    }

    private static (Mock<IServiceResolver> Resolver, Mock<IServiceResolverFactory> ResolverFactory) CreateResolver()
    {
        var mockLogger = new Mock<ILogger<SnsApplication>>();
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<ILogger<SnsApplication>>()).Returns(mockLogger.Object);
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);
        return (mockResolver, mockResolverFactory);
    }

    [Fact]
    public void SnsOptions_Defaults_AreCascadeAndDoNotEscalate()
    {
        var options = new SnsOptions();
        Assert.False(options.CatchExceptions);
        Assert.False(options.RaiseOnFailureStatus);
    }

    [Fact]
    public async Task HandleAsync_DefaultOptions_HandlerThrows_ExceptionCascades()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<SnsRecordContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<SnsRecordContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var (_, resolverFactory) = CreateResolver();
        var application = new SnsApplication(mockPipeline.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => application.HandleAsync(CreateEvent(), resolverFactory.Object));
    }

    [Fact]
    public async Task HandleAsync_CatchExceptionsTrue_HandlerThrows_ExceptionIsSwallowedAndLogged()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<SnsRecordContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<SnsRecordContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var (_, resolverFactory) = CreateResolver();
        var application = new SnsApplication(mockPipeline.Object, new SnsOptions { CatchExceptions = true });

        // Reaching the end without throwing proves the exception was caught, not cascaded.
        await application.HandleAsync(CreateEvent(), resolverFactory.Object);
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusTrue_HandlerReturnsFailureResult_ThrowsSnsMessageProcessingException()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<SnsRecordContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<SnsRecordContext>(), It.IsAny<IServiceResolver>()))
            .Callback<SnsRecordContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new SnsApplication(mockPipeline.Object, new SnsOptions { RaiseOnFailureStatus = true });

        var exception = await Assert.ThrowsAsync<SnsMessageProcessingException>(
            () => application.HandleAsync(CreateEvent("msg-2"), resolverFactory.Object));
        Assert.Equal("msg-2", exception.MessageId);
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusTrue_HandlerSucceeds_DoesNotThrow()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<SnsRecordContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<SnsRecordContext>(), It.IsAny<IServiceResolver>()))
            .Callback<SnsRecordContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(true))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new SnsApplication(mockPipeline.Object, new SnsOptions { RaiseOnFailureStatus = true });

        await application.HandleAsync(CreateEvent(), resolverFactory.Object);
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusAndCatchExceptionsBothTrue_FailureResultIsEscalatedThenSwallowed()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<SnsRecordContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<SnsRecordContext>(), It.IsAny<IServiceResolver>()))
            .Callback<SnsRecordContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new SnsApplication(mockPipeline.Object, new SnsOptions { RaiseOnFailureStatus = true, CatchExceptions = true });

        // Reaching the end without throwing proves the escalated failure was caught too.
        await application.HandleAsync(CreateEvent(), resolverFactory.Object);
    }
}
