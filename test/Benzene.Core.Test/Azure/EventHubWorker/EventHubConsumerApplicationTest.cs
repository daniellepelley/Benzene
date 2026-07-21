using System;
using Benzene.Results;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.EventHub;
using Benzene.Core.MessageHandlers;
using Moq;
using Xunit;

namespace Benzene.Test.Azure.EventHubWorker;

public class EventHubConsumerApplicationTest
{
    private static EventData CreateEvent()
    {
        return new EventData(new BinaryData("some-message"));
    }

    private static Mock<IServiceResolverFactory> CreateResolverFactory()
    {
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);
        return mockResolverFactory;
    }

    [Fact]
    public async Task HandleAsync_MapsEventToContext_ReturnsRecordedResult()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventHubConsumerContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventHubConsumerContext>(), It.IsAny<IServiceResolver>()))
            .Callback<EventHubConsumerContext, IServiceResolver>((context, _) => context.MessageResult = BenzeneResult.Ok())
            .Returns(Task.CompletedTask);

        var application = new EventHubConsumerApplication(mockPipeline.Object);

        var result = await application.HandleAsync(CreateEvent(), CreateResolverFactory().Object);

        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public async Task HandleAsync_NothingRecordsResult_ReturnsNull()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventHubConsumerContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventHubConsumerContext>(), It.IsAny<IServiceResolver>()))
            .Returns(Task.CompletedTask);

        var application = new EventHubConsumerApplication(mockPipeline.Object);

        var result = await application.HandleAsync(CreateEvent(), CreateResolverFactory().Object);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_PipelineThrows_ExceptionPropagates()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventHubConsumerContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventHubConsumerContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var application = new EventHubConsumerApplication(mockPipeline.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => application.HandleAsync(CreateEvent(), CreateResolverFactory().Object));
    }

    [Fact]
    public void BenzeneEventHubConfig_Defaults()
    {
        var config = new BenzeneEventHubConfig();

        Assert.Equal(1, config.CheckpointInterval);
        Assert.True(config.CatchHandlerExceptions);
        Assert.Null(config.DefaultStartingPosition);
    }
}
