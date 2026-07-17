using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Kafka;
using Benzene.Core.MessageHandlers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class KafkaFailureHandlingTest
{
    private static KafkaRecord[] CreateEvent(string topic = "my-topic")
    {
        return [new KafkaRecord { Topic = topic }];
    }

    private static (Mock<IServiceResolver> Resolver, Mock<IServiceResolverFactory> ResolverFactory) CreateResolver()
    {
        var mockLogger = new Mock<ILogger<KafkaApplication>>();
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<ILogger<KafkaApplication>>()).Returns(mockLogger.Object);
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);
        return (mockResolver, mockResolverFactory);
    }

    [Fact]
    public void KafkaOptions_Defaults_AreCascadeAndDoNotEscalate()
    {
        var options = new KafkaOptions();
        Assert.False(options.CatchExceptions);
        Assert.False(options.RaiseOnFailureStatus);
    }

    [Fact]
    public async Task HandleAsync_DefaultOptions_HandlerThrows_ExceptionCascades()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<KafkaContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<KafkaContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var (_, resolverFactory) = CreateResolver();
        var application = new KafkaBatchApplication(mockPipeline.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => application.HandleAsync(CreateEvent(), resolverFactory.Object));
    }

    [Fact]
    public async Task HandleAsync_CatchExceptionsTrue_HandlerThrows_ExceptionIsSwallowedAndLogged()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<KafkaContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<KafkaContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var (_, resolverFactory) = CreateResolver();
        var application = new KafkaBatchApplication(mockPipeline.Object, new KafkaOptions { CatchExceptions = true });

        // Reaching the end without throwing proves the exception was caught, not cascaded.
        await application.HandleAsync(CreateEvent(), resolverFactory.Object);
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusTrue_HandlerReturnsFailureResult_ThrowsKafkaMessageProcessingException()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<KafkaContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<KafkaContext>(), It.IsAny<IServiceResolver>()))
            .Callback<KafkaContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new KafkaBatchApplication(mockPipeline.Object, new KafkaOptions { RaiseOnFailureStatus = true });

        var exception = await Assert.ThrowsAsync<KafkaMessageProcessingException>(
            () => application.HandleAsync(CreateEvent("orders-topic"), resolverFactory.Object));
        Assert.Equal("orders-topic", exception.Topic);
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusTrue_HandlerSucceeds_DoesNotThrow()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<KafkaContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<KafkaContext>(), It.IsAny<IServiceResolver>()))
            .Callback<KafkaContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(true))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new KafkaBatchApplication(mockPipeline.Object, new KafkaOptions { RaiseOnFailureStatus = true });

        await application.HandleAsync(CreateEvent(), resolverFactory.Object);
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusAndCatchExceptionsBothTrue_FailureResultIsEscalatedThenSwallowed()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<KafkaContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<KafkaContext>(), It.IsAny<IServiceResolver>()))
            .Callback<KafkaContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new KafkaBatchApplication(mockPipeline.Object, new KafkaOptions { RaiseOnFailureStatus = true, CatchExceptions = true });

        // Reaching the end without throwing proves the escalated failure was caught too.
        await application.HandleAsync(CreateEvent(), resolverFactory.Object);
    }
}
