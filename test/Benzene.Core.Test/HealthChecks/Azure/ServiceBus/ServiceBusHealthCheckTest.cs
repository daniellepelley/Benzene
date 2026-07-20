using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Benzene.HealthChecks.Azure.ServiceBus;
using Benzene.HealthChecks.Core;
using Moq;
using Xunit;

namespace Benzene.Test.HealthChecks.Azure.ServiceBus;

public class ServiceBusHealthCheckTest
{
    [Fact]
    public async Task ExecuteAsync_QueuePeekSucceeds_ReturnsHealthy_WithTheQueueDependency()
    {
        var receiver = new Mock<ServiceBusReceiver>();
        receiver.Setup(x => x.PeekMessageAsync(It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceBusReceivedMessage?)null); // empty queue - still a successful round-trip

        var client = new Mock<ServiceBusClient>();
        client.Setup(x => x.CreateReceiver("orders")).Returns(receiver.Object);

        var result = await new ServiceBusHealthCheck(client.Object, "orders").ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("ServiceBus", result.Type);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("Queue", dependency.Kind);
        Assert.Equal("orders", dependency.Name);
    }

    [Fact]
    public async Task ExecuteAsync_SubscriptionPeekSucceeds_ReturnsHealthy_WithTheSubscriptionDependency()
    {
        var receiver = new Mock<ServiceBusReceiver>();
        receiver.Setup(x => x.PeekMessageAsync(It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceBusReceivedMessage?)null);

        var client = new Mock<ServiceBusClient>();
        client.Setup(x => x.CreateReceiver("events", "audit")).Returns(receiver.Object);

        var result = await new ServiceBusHealthCheck(client.Object, "events", "audit").ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("Subscription", dependency.Kind);
        Assert.Equal("events/audit", dependency.Name);
    }

    [Fact]
    public async Task ExecuteAsync_PeekThrows_ReturnsUnhealthy_ReportingTheExceptionTypeNotMessage()
    {
        var receiver = new Mock<ServiceBusReceiver>();
        receiver.Setup(x => x.PeekMessageAsync(It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("entity not found: super-secret-connection-detail",
                ServiceBusFailureReason.MessagingEntityNotFound));

        var client = new Mock<ServiceBusClient>();
        client.Setup(x => x.CreateReceiver("orders")).Returns(receiver.Object);

        var result = await new ServiceBusHealthCheck(client.Object, "orders").ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal("ServiceBusException", result.Data["Error"]);
        // The exception message (which could carry sensitive detail) must never reach Data.
        Assert.DoesNotContain(result.Data.Values,
            v => v is string s && s.Contains("super-secret-connection-detail"));
        Assert.Equal("orders", Assert.Single(result.Dependencies).Name);
    }

    [Fact]
    public async Task ExecuteAsync_DisposesTheReceiver_EvenWhenPeekThrows()
    {
        var receiver = new Mock<ServiceBusReceiver>();
        receiver.Setup(x => x.PeekMessageAsync(It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("boom", ServiceBusFailureReason.ServiceCommunicationProblem));

        var client = new Mock<ServiceBusClient>();
        client.Setup(x => x.CreateReceiver("orders")).Returns(receiver.Object);

        await new ServiceBusHealthCheck(client.Object, "orders").ExecuteAsync();

        receiver.Verify(x => x.DisposeAsync(), Times.Once);
    }
}
