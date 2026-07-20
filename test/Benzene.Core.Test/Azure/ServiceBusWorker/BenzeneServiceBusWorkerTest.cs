using System;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.ServiceBus;
using Moq;
using Xunit;

namespace Benzene.Test.Azure.ServiceBusWorker;

public class BenzeneServiceBusWorkerTest
{
    private static BenzeneServiceBusWorker CreateWorker(BenzeneServiceBusConfig config, Mock<IServiceBusClientFactory> mockClientFactory)
    {
        var application = new ServiceBusConsumerApplication(Mock.Of<IMiddlewarePipeline<ServiceBusConsumerContext>>());
        return new BenzeneServiceBusWorker(Mock.Of<IServiceResolverFactory>(), application, config, mockClientFactory.Object);
    }

    [Fact]
    public void BenzeneServiceBusConfig_Defaults()
    {
        var config = new BenzeneServiceBusConfig();

        Assert.Equal(ServiceBusConsumerAckMode.Explicit, config.AckMode);
        Assert.Equal(5, config.MaxConcurrentCalls);
        Assert.Equal(0, config.PrefetchCount);
    }

    [Fact]
    public async Task StartAsync_NoEntityConfigured_ThrowsWithoutCreatingClient()
    {
        var mockClientFactory = new Mock<IServiceBusClientFactory>();
        var worker = CreateWorker(new BenzeneServiceBusConfig(), mockClientFactory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => worker.StartAsync(CancellationToken.None));

        mockClientFactory.Verify(x => x.Create(), Times.Never);
    }

    [Fact]
    public async Task StartAsync_BothQueueAndSubscriptionConfigured_Throws()
    {
        var config = new BenzeneServiceBusConfig
        {
            QueueName = "some-queue",
            TopicName = "some-topic",
            SubscriptionName = "some-subscription"
        };
        var worker = CreateWorker(config, new Mock<IServiceBusClientFactory>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => worker.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_TopicWithoutSubscription_Throws()
    {
        var config = new BenzeneServiceBusConfig { TopicName = "some-topic" };
        var worker = CreateWorker(config, new Mock<IServiceBusClientFactory>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => worker.StartAsync(CancellationToken.None));
    }
}
