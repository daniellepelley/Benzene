using System;
using System.Text;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.RabbitMq.RabbitMqMessage;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace Benzene.Test.RabbitMq;

public class RabbitMqApplicationTest
{
    private static BasicDeliverEventArgs CreateDelivery()
    {
        return new BasicDeliverEventArgs("tag", 1, false, "exchange", "orderCreated",
            new BasicProperties(), Encoding.UTF8.GetBytes("{}"));
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
    public async Task HandleAsync_MapsDeliveryToContext_ReturnsRecordedResult()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<RabbitMqContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.Is<RabbitMqContext>(c => c.DeliverEventArgs.RoutingKey == "orderCreated"), It.IsAny<IServiceResolver>()))
            .Callback<RabbitMqContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var application = new RabbitMqApplication(mockPipeline.Object);

        var result = await application.HandleAsync(CreateDelivery(), CreateResolverFactory().Object);

        Assert.NotNull(result);
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public async Task HandleAsync_NothingRecordsResult_ReturnsNull()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<RabbitMqContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<RabbitMqContext>(), It.IsAny<IServiceResolver>()))
            .Returns(Task.CompletedTask);

        var application = new RabbitMqApplication(mockPipeline.Object);

        var result = await application.HandleAsync(CreateDelivery(), CreateResolverFactory().Object);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_PipelineThrows_ExceptionPropagates()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<RabbitMqContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<RabbitMqContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var application = new RabbitMqApplication(mockPipeline.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => application.HandleAsync(CreateDelivery(), CreateResolverFactory().Object));
    }
}
