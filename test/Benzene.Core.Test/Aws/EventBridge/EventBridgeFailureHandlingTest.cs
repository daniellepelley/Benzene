using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.EventBridge;
using Benzene.Core.MessageHandlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.EventBridge;

public class EventBridgeFailureHandlingTest
{
    private static EventBridgeEvent CreateEvent(string id = "evt-1")
        => new EventBridgeEvent { Id = id, DetailType = "OrderPlaced", Source = "orders" };

    private static Mock<IServiceResolverFactory> CreateResolverFactory()
    {
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<ILogger<EventBridgeApplication>>()).Returns(Mock.Of<ILogger<EventBridgeApplication>>());
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);
        return mockResolverFactory;
    }

    [Fact]
    public void EventBridgeOptions_Defaults_AreCascadeAndDoNotEscalate()
    {
        var options = new EventBridgeOptions();
        Assert.False(options.CatchExceptions);
        Assert.False(options.RaiseOnFailureStatus);
    }

    [Fact]
    public async Task HandleAsync_DefaultOptions_HandlerThrows_ExceptionCascades()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventBridgeContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventBridgeContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var application = new EventBridgeApplication(mockPipeline.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => application.HandleAsync(CreateEvent(), CreateResolverFactory().Object));
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusTrue_HandlerReturnsFailureResult_ThrowsEventBridgeMessageProcessingException()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventBridgeContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventBridgeContext>(), It.IsAny<IServiceResolver>()))
            .Callback<EventBridgeContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var application = new EventBridgeApplication(mockPipeline.Object, new EventBridgeOptions { RaiseOnFailureStatus = true });

        var exception = await Assert.ThrowsAsync<EventBridgeMessageProcessingException>(
            () => application.HandleAsync(CreateEvent("evt-2"), CreateResolverFactory().Object));
        Assert.Equal("evt-2", exception.EventId);
    }

    [Fact]
    public async Task HandleAsync_DefaultOptions_HandlerReturnsFailureResult_DoesNotThrow()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventBridgeContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventBridgeContext>(), It.IsAny<IServiceResolver>()))
            .Callback<EventBridgeContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var application = new EventBridgeApplication(mockPipeline.Object);

        await application.HandleAsync(CreateEvent(), CreateResolverFactory().Object);
    }
}
