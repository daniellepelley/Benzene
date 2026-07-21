using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.EventHub.Function;
using Benzene.Core.MessageHandlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class EventHubFailureHandlingTest
{
    private static EventData[] CreateEvent(long sequenceNumber = 1, string body = "body")
        => [EventHubsModelFactory.EventData(new BinaryData(body), sequenceNumber: sequenceNumber)];

    private static Mock<IServiceResolverFactory> CreateResolverFactory()
    {
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<ILogger<EventHubApplication>>()).Returns(Mock.Of<ILogger<EventHubApplication>>());
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);
        return mockResolverFactory;
    }

    [Fact]
    public void EventHubOptions_Defaults_CascadeExceptions_AndEscalateFailureResults()
    {
        var options = new EventHubOptions();
        Assert.False(options.CatchExceptions);
        Assert.True(options.RaiseOnFailureStatus);
        Assert.Null(options.MaxDegreeOfParallelism);
    }

    [Fact]
    public async Task HandleAsync_DefaultOptions_HandlerThrows_ExceptionCascades()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventHubContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventHubContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var application = new EventHubBatchApplication(mockPipeline.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => application.HandleAsync(CreateEvent(), CreateResolverFactory().Object));
    }

    [Fact]
    public async Task HandleAsync_CatchExceptionsTrue_HandlerThrows_IsContained()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventHubContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventHubContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var application = new EventHubBatchApplication(mockPipeline.Object, new EventHubOptions { CatchExceptions = true });

        // Does not throw - the poison event is logged and skipped.
        await application.HandleAsync(CreateEvent(), CreateResolverFactory().Object);
    }

    [Fact]
    public async Task HandleAsync_CatchExceptionsTrue_PoisonEvent_DoesNotFailSiblings()
    {
        var processed = new ConcurrentBag<string>();
        var mockPipeline = new Mock<IMiddlewarePipeline<EventHubContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventHubContext>(), It.IsAny<IServiceResolver>()))
            .Returns<EventHubContext, IServiceResolver>((context, _) =>
            {
                var body = context.EventData.EventBody.ToString();
                if (body == "poison")
                {
                    throw new InvalidOperationException("boom");
                }

                processed.Add(body);
                return Task.CompletedTask;
            });

        EventData[] batch =
        [
            EventHubsModelFactory.EventData(new BinaryData("good-1"), sequenceNumber: 1),
            EventHubsModelFactory.EventData(new BinaryData("poison"), sequenceNumber: 2),
            EventHubsModelFactory.EventData(new BinaryData("good-2"), sequenceNumber: 3),
        ];

        var application = new EventHubBatchApplication(mockPipeline.Object, new EventHubOptions { CatchExceptions = true });

        await application.HandleAsync(batch, CreateResolverFactory().Object);

        Assert.Contains("good-1", processed);
        Assert.Contains("good-2", processed);
        Assert.DoesNotContain("poison", processed);
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusTrue_HandlerReturnsFailureResult_ThrowsEventHubMessageProcessingException()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventHubContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventHubContext>(), It.IsAny<IServiceResolver>()))
            .Callback<EventHubContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var application = new EventHubBatchApplication(mockPipeline.Object, new EventHubOptions { RaiseOnFailureStatus = true });

        var exception = await Assert.ThrowsAsync<EventHubMessageProcessingException>(
            () => application.HandleAsync(CreateEvent(sequenceNumber: 7), CreateResolverFactory().Object));
        Assert.Equal("7", exception.SequenceNumber);
    }

    [Fact]
    public async Task HandleAsync_DefaultOptions_HandlerReturnsFailureResult_ThrowsEventHubMessageProcessingException()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventHubContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventHubContext>(), It.IsAny<IServiceResolver>()))
            .Callback<EventHubContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var application = new EventHubBatchApplication(mockPipeline.Object);

        // Safe-by-default: a returned failure result is escalated so the batch isn't checkpointed past it.
        await Assert.ThrowsAsync<EventHubMessageProcessingException>(
            () => application.HandleAsync(CreateEvent(), CreateResolverFactory().Object));
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusTrue_HandlerSucceeds_DoesNotThrow()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventHubContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventHubContext>(), It.IsAny<IServiceResolver>()))
            .Callback<EventHubContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(true))
            .Returns(Task.CompletedTask);

        var application = new EventHubBatchApplication(mockPipeline.Object, new EventHubOptions { RaiseOnFailureStatus = true });

        await application.HandleAsync(CreateEvent(), CreateResolverFactory().Object);
    }
}
