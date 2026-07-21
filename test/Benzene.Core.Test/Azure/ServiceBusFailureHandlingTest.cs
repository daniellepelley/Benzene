using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.ServiceBus;
using Benzene.Core.MessageHandlers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class ServiceBusFailureHandlingTest
{
    private static ServiceBusReceivedMessage[] CreateEvent(string messageId = "msg-1")
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: messageId);
        return [message];
    }

    private static (Mock<IServiceResolver> Resolver, Mock<IServiceResolverFactory> ResolverFactory) CreateResolver()
    {
        var mockLogger = new Mock<ILogger<ServiceBusApplication>>();
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<ILogger<ServiceBusApplication>>()).Returns(mockLogger.Object);
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);
        return (mockResolver, mockResolverFactory);
    }

    [Fact]
    public void ServiceBusOptions_Defaults_CascadeExceptions_AndEscalateFailureResults()
    {
        var options = new ServiceBusOptions();
        Assert.False(options.CatchExceptions);
        Assert.True(options.RaiseOnFailureStatus);
    }

    [Fact]
    public async Task HandleAsync_DefaultOptions_HandlerThrows_ExceptionCascades()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var (_, resolverFactory) = CreateResolver();
        var application = new ServiceBusBatchApplication(mockPipeline.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => application.HandleAsync(CreateEvent(), resolverFactory.Object));
    }

    [Fact]
    public async Task HandleAsync_CatchExceptionsTrue_HandlerThrows_ExceptionIsSwallowedAndLogged()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var (_, resolverFactory) = CreateResolver();
        var application = new ServiceBusBatchApplication(mockPipeline.Object, new ServiceBusOptions { CatchExceptions = true });

        // Reaching the end without throwing proves the exception was caught, not cascaded.
        await application.HandleAsync(CreateEvent(), resolverFactory.Object);
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusTrue_HandlerReturnsFailureResult_ThrowsServiceBusMessageProcessingException()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusContext>(), It.IsAny<IServiceResolver>()))
            .Callback<ServiceBusContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new ServiceBusBatchApplication(mockPipeline.Object, new ServiceBusOptions { RaiseOnFailureStatus = true });

        var exception = await Assert.ThrowsAsync<ServiceBusMessageProcessingException>(
            () => application.HandleAsync(CreateEvent("msg-2"), resolverFactory.Object));
        Assert.Equal("msg-2", exception.MessageId);
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusTrue_HandlerSucceeds_DoesNotThrow()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusContext>(), It.IsAny<IServiceResolver>()))
            .Callback<ServiceBusContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(true))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new ServiceBusBatchApplication(mockPipeline.Object, new ServiceBusOptions { RaiseOnFailureStatus = true });

        await application.HandleAsync(CreateEvent(), resolverFactory.Object);
    }

    [Fact]
    public async Task HandleAsync_RaiseOnFailureStatusAndCatchExceptionsBothTrue_FailureResultIsEscalatedThenSwallowed()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusContext>(), It.IsAny<IServiceResolver>()))
            .Callback<ServiceBusContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new ServiceBusBatchApplication(mockPipeline.Object, new ServiceBusOptions { RaiseOnFailureStatus = true, CatchExceptions = true });

        // Reaching the end without throwing proves the escalated failure was caught too.
        await application.HandleAsync(CreateEvent(), resolverFactory.Object);
    }

    [Fact]
    public void ServiceBusOptions_AckMode_DefaultsToAutoComplete()
    {
        Assert.Equal(ServiceBusAckMode.AutoComplete, new ServiceBusOptions().AckMode);
    }

    [Fact]
    public async Task HandleAsync_ExplicitAckMode_HandlerSucceeds_CompletesMessage()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusContext>(), It.IsAny<IServiceResolver>()))
            .Callback<ServiceBusContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(true))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var mockActions = new Mock<ServiceBusMessageActions>();
        var application = new ServiceBusBatchApplication(mockPipeline.Object, new ServiceBusOptions { AckMode = ServiceBusAckMode.Explicit });
        var message = CreateEvent()[0];

        await ((IMiddlewareApplication<ServiceBusTriggerBatch>)application)
            .HandleAsync(new ServiceBusTriggerBatch(mockActions.Object, [message]), resolverFactory.Object);

        mockActions.Verify(x => x.CompleteMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        mockActions.Verify(x => x.AbandonMessageAsync(message, null, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ExplicitAckMode_HandlerReturnsFailureResult_AbandonsMessage()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusContext>(), It.IsAny<IServiceResolver>()))
            .Callback<ServiceBusContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(false))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var mockActions = new Mock<ServiceBusMessageActions>();
        var application = new ServiceBusBatchApplication(mockPipeline.Object, new ServiceBusOptions { AckMode = ServiceBusAckMode.Explicit });
        var message = CreateEvent()[0];

        await ((IMiddlewareApplication<ServiceBusTriggerBatch>)application)
            .HandleAsync(new ServiceBusTriggerBatch(mockActions.Object, [message]), resolverFactory.Object);

        mockActions.Verify(x => x.AbandonMessageAsync(message, null, It.IsAny<CancellationToken>()), Times.Once);
        mockActions.Verify(x => x.CompleteMessageAsync(message, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ExplicitAckMode_HandlerThrows_AbandonsMessageThenCascades()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var (_, resolverFactory) = CreateResolver();
        var mockActions = new Mock<ServiceBusMessageActions>();
        var application = new ServiceBusBatchApplication(mockPipeline.Object, new ServiceBusOptions { AckMode = ServiceBusAckMode.Explicit });
        var message = CreateEvent()[0];

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ((IMiddlewareApplication<ServiceBusTriggerBatch>)application)
                .HandleAsync(new ServiceBusTriggerBatch(mockActions.Object, [message]), resolverFactory.Object));

        mockActions.Verify(x => x.AbandonMessageAsync(message, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ExplicitAckModeAndCatchExceptionsTrue_HandlerThrows_AbandonsMessageAndSwallows()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusContext>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var (_, resolverFactory) = CreateResolver();
        var mockActions = new Mock<ServiceBusMessageActions>();
        var application = new ServiceBusBatchApplication(mockPipeline.Object, new ServiceBusOptions { AckMode = ServiceBusAckMode.Explicit, CatchExceptions = true });
        var message = CreateEvent()[0];

        // Reaching the end without throwing proves the exception was caught, not cascaded.
        await ((IMiddlewareApplication<ServiceBusTriggerBatch>)application)
            .HandleAsync(new ServiceBusTriggerBatch(mockActions.Object, [message]), resolverFactory.Object);

        mockActions.Verify(x => x.AbandonMessageAsync(message, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReceivedMessageArrayOverload_NeverTouchesMessageActions()
    {
        // The plain ServiceBusReceivedMessage[] overload (no ServiceBusMessageActions available)
        // must behave exactly as AutoComplete mode always has, even when AckMode is Explicit.
        var mockPipeline = new Mock<IMiddlewarePipeline<ServiceBusContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<ServiceBusContext>(), It.IsAny<IServiceResolver>()))
            .Callback<ServiceBusContext, IServiceResolver>((context, _) => context.MessageResult = new MessageResult(true))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new ServiceBusBatchApplication(mockPipeline.Object, new ServiceBusOptions { AckMode = ServiceBusAckMode.Explicit });

        // No ServiceBusMessageActions involved at all - proves this overload doesn't require one.
        await application.HandleAsync(CreateEvent(), resolverFactory.Object);
    }
}
