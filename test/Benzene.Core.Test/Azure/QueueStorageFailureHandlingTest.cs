using System;
using Benzene.Results;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.QueueStorage;
using Benzene.Core.MessageHandlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class QueueStorageFailureHandlingTest
{
    private static QueueStorageMessage[] CreateEvent(string id = "msg-1")
        => [new QueueStorageMessage("body") { MessageId = id }];

    private static Mock<IServiceResolverFactory> CreateResolverFactory()
    {
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<ILogger<QueueStorageApplication>>()).Returns(Mock.Of<ILogger<QueueStorageApplication>>());
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);
        return mockResolverFactory;
    }

    [Fact]
    public void QueueStorageOptions_Defaults_CascadeExceptions_AndEscalateFailureResults()
    {
        var options = new QueueStorageOptions();
        Assert.False(options.CatchExceptions);
        Assert.True(options.RaiseOnFailureStatus);
    }

    [Fact]
    public async Task HandleAsync_DefaultOptions_HandlerThrows_ExceptionCascades()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<QueueStorageContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<QueueStorageContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var application = new QueueStorageBatchApplication(mockPipeline.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => application.HandleAsync(CreateEvent(), CreateResolverFactory().Object));
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusTrue_HandlerReturnsFailureResult_ThrowsQueueStorageMessageProcessingException()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<QueueStorageContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<QueueStorageContext>(), It.IsAny<IServiceResolver>()))
            .Callback<QueueStorageContext, IServiceResolver>((context, _) => context.MessageResult = BenzeneResult.UnexpectedError())
            .Returns(Task.CompletedTask);

        var application = new QueueStorageBatchApplication(mockPipeline.Object, new QueueStorageOptions { RaiseOnFailureStatus = true });

        var exception = await Assert.ThrowsAsync<QueueStorageMessageProcessingException>(
            () => application.HandleAsync(CreateEvent("msg-2"), CreateResolverFactory().Object));
        Assert.Equal("msg-2", exception.MessageId);
    }

    [Fact]
    public async Task HandleAsync_DefaultOptions_HandlerReturnsFailureResult_ThrowsQueueStorageMessageProcessingException()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<QueueStorageContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<QueueStorageContext>(), It.IsAny<IServiceResolver>()))
            .Callback<QueueStorageContext, IServiceResolver>((context, _) => context.MessageResult = BenzeneResult.UnexpectedError())
            .Returns(Task.CompletedTask);

        var application = new QueueStorageBatchApplication(mockPipeline.Object);

        // Safe-by-default: a returned failure result is escalated so the message is redelivered.
        await Assert.ThrowsAsync<QueueStorageMessageProcessingException>(
            () => application.HandleAsync(CreateEvent(), CreateResolverFactory().Object));
    }
}
