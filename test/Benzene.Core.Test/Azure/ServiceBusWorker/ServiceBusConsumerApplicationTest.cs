using System;
using Benzene.Results;
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
            .Callback<ServiceBusConsumerContext, IServiceResolver>((context, _) => context.MessageResult = BenzeneResult.UnexpectedError())
            .Returns(Task.CompletedTask);

        var application = new ServiceBusConsumerApplication(mockPipeline.Object);

        var result = await application.HandleAsync(CreateMessage(), CreateResolverFactory().Object);

        Assert.NotNull(result.MessageResult);
        Assert.False(result.MessageResult.IsSuccessful);
    }

    [Fact]
    public async Task HandleAsync_NothingRecordsResult_ReturnsNull()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusConsumerContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusConsumerContext>(), It.IsAny<IServiceResolver>()))
            .Returns(Task.CompletedTask);

        var application = new ServiceBusConsumerApplication(mockPipeline.Object);

        var result = await application.HandleAsync(CreateMessage(), CreateResolverFactory().Object);

        Assert.Null(result.MessageResult);
    }

    [Fact]
    public async Task HandleAsync_HandlerRequestsDeadLetter_SurfacesTheOverrideInTheDecision()
    {
        var holder = new ServiceBusSettlementHolder();

        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.TryGetService<ServiceBusSettlementHolder>()).Returns(holder);
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);

        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusConsumerContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusConsumerContext>(), It.IsAny<IServiceResolver>()))
            .Callback<ServiceBusConsumerContext, IServiceResolver>((_, __) =>
            {
                // A handler resolves the scoped holder and requests dead-lettering.
                holder.Override = ServiceBusSettlement.DeadLetter;
                holder.DeadLetterReason = "unprocessable";
            })
            .Returns(Task.CompletedTask);

        var application = new ServiceBusConsumerApplication(mockPipeline.Object);

        var result = await application.HandleAsync(CreateMessage(), mockResolverFactory.Object);

        Assert.Equal(ServiceBusSettlement.DeadLetter, result.Settlement!.Override);
        Assert.Equal("unprocessable", result.Settlement.DeadLetterReason);
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
