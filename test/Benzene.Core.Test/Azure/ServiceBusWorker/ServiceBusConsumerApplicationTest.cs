using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.ServiceBus;
using Benzene.Core.MessageHandlers;
using Moq;
using Xunit;

namespace Benzene.Test.Azure.ServiceBusWorker;

public class ServiceBusConsumerApplicationTest
{
    private static ServiceBusReceivedMessage CreateMessage(string messageId = "msg-1")
    {
        return ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: messageId);
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
    public async Task HandleAsync_MapsMessageToContext_ReturnsRecordedResult()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusConsumerContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.Is<ServiceBusConsumerContext>(c => c.Message.MessageId == "msg-1"), It.IsAny<IServiceResolver>()))
            .Callback<ServiceBusConsumerContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var application = new ServiceBusConsumerApplication(mockPipeline.Object);

        var result = await application.HandleAsync(CreateMessage(), CreateResolverFactory().Object);

        Assert.NotNull(result);
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public async Task HandleAsync_NothingRecordsResult_ReturnsNull()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusConsumerContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusConsumerContext>(), It.IsAny<IServiceResolver>()))
            .Returns(Task.CompletedTask);

        var application = new ServiceBusConsumerApplication(mockPipeline.Object);

        var result = await application.HandleAsync(CreateMessage(), CreateResolverFactory().Object);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_PipelineThrows_ExceptionPropagates()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusConsumerContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusConsumerContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var application = new ServiceBusConsumerApplication(mockPipeline.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => application.HandleAsync(CreateMessage(), CreateResolverFactory().Object));
    }
}
