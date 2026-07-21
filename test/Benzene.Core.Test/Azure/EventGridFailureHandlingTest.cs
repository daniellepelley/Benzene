using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.EventGrid;
using Benzene.Core.MessageHandlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class EventGridFailureHandlingTest
{
    private static EventGridTriggerEvent[] CreateEvent(string id = "evt-1")
        => [new EventGridTriggerEvent { Id = id, EventType = "OrderPlaced" }];

    private static Mock<IServiceResolverFactory> CreateResolverFactory()
    {
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<ILogger<EventGridApplication>>()).Returns(Mock.Of<ILogger<EventGridApplication>>());
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);
        return mockResolverFactory;
    }

    [Fact]
    public void EventGridOptions_Defaults_CascadeExceptions_AndEscalateFailureResults()
    {
        var options = new EventGridOptions();
        Assert.False(options.CatchExceptions);
        Assert.True(options.RaiseOnFailureStatus);
    }

    [Fact]
    public async Task HandleAsync_DefaultOptions_HandlerThrows_ExceptionCascades()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventGridContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventGridContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var application = new EventGridBatchApplication(mockPipeline.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => application.HandleAsync(CreateEvent(), CreateResolverFactory().Object));
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusTrue_HandlerReturnsFailureResult_ThrowsEventGridMessageProcessingException()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventGridContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventGridContext>(), It.IsAny<IServiceResolver>()))
            .Callback<EventGridContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var application = new EventGridBatchApplication(mockPipeline.Object, new EventGridOptions { RaiseOnFailureStatus = true });

        var exception = await Assert.ThrowsAsync<EventGridMessageProcessingException>(
            () => application.HandleAsync(CreateEvent("evt-2"), CreateResolverFactory().Object));
        Assert.Equal("evt-2", exception.EventId);
    }

    [Fact]
    public async Task HandleAsync_DefaultOptions_HandlerReturnsFailureResult_ThrowsEventGridMessageProcessingException()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventGridContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventGridContext>(), It.IsAny<IServiceResolver>()))
            .Callback<EventGridContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var application = new EventGridBatchApplication(mockPipeline.Object);

        // Safe-by-default: a returned failure result is escalated so Event Grid retries it.
        await Assert.ThrowsAsync<EventGridMessageProcessingException>(
            () => application.HandleAsync(CreateEvent(), CreateResolverFactory().Object));
    }
}
