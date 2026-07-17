using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.CosmosDb;
using Benzene.Core.Middleware;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Azure.CosmosDbWorker;

public class BenzeneCosmosChangeFeedWorkerTest
{
    // Public: this type parameterizes ILogger<BenzeneCosmosChangeFeedWorker<OrderDocument>>, which
    // Moq can only proxy when every generic argument is accessible.
    public class OrderDocument
    {
        public string Id { get; set; }
    }

    private class Harness
    {
        public Mock<IMiddlewarePipeline<StreamContext<OrderDocument>>> MockPipeline { get; } = new();
        public Mock<ChangeFeedProcessor> MockProcessor { get; } = new();
        public Mock<ICosmosChangeFeedProcessorFactory<OrderDocument>> MockFactory { get; } = new();
        public BenzeneCosmosChangeFeedWorker<OrderDocument> Worker { get; }
        public Container.ChangeFeedHandlerWithManualCheckpoint<OrderDocument> OnChanges { get; private set; }
        public Container.ChangeFeedMonitorErrorDelegate OnError { get; private set; }
        public int CheckpointCalls { get; private set; }

        public Harness(BenzeneCosmosChangeFeedConfig config)
        {
            MockProcessor.Setup(x => x.StartAsync()).Returns(Task.CompletedTask);
            MockProcessor.Setup(x => x.StopAsync()).Returns(Task.CompletedTask);
            MockFactory
                .Setup(x => x.Create(
                    It.IsAny<Container.ChangeFeedHandlerWithManualCheckpoint<OrderDocument>>(),
                    It.IsAny<Container.ChangeFeedMonitorErrorDelegate>()))
                .Callback<Container.ChangeFeedHandlerWithManualCheckpoint<OrderDocument>, Container.ChangeFeedMonitorErrorDelegate>(
                    (onChanges, onError) =>
                    {
                        OnChanges = onChanges;
                        OnError = onError;
                    })
                .Returns(MockProcessor.Object);

            var mockResolver = new Mock<IServiceResolver>();
            mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
            mockResolver.Setup(x => x.GetService<ILogger<BenzeneCosmosChangeFeedWorker<OrderDocument>>>())
                .Returns(Mock.Of<ILogger<BenzeneCosmosChangeFeedWorker<OrderDocument>>>());
            var mockResolverFactory = new Mock<IServiceResolverFactory>();
            mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);

            var application = new CosmosChangeFeedApplication<OrderDocument>(MockPipeline.Object);
            Worker = new BenzeneCosmosChangeFeedWorker<OrderDocument>(
                mockResolverFactory.Object, application, config, MockFactory.Object);
        }

        public Task DeliverBatchAsync(params string[] ids)
        {
            var documents = new List<OrderDocument>();
            foreach (var id in ids)
            {
                documents.Add(new OrderDocument { Id = id });
            }

            var mockContext = new Mock<ChangeFeedProcessorContext>();
            mockContext.Setup(x => x.LeaseToken).Returns("0");

            return OnChanges(mockContext.Object, documents,
                () => { CheckpointCalls++; return Task.CompletedTask; }, CancellationToken.None);
        }
    }

    [Fact]
    public void BenzeneCosmosChangeFeedConfig_Defaults()
    {
        var config = new BenzeneCosmosChangeFeedConfig();

        Assert.True(config.AutoCheckpointOnSuccess);
        Assert.False(config.CatchHandlerExceptions);
    }

    [Fact]
    public async Task StartAsync_CreatesProcessorWithHandlers_AndStartsIt()
    {
        var harness = new Harness(new BenzeneCosmosChangeFeedConfig());

        await harness.Worker.StartAsync(CancellationToken.None);

        Assert.NotNull(harness.OnChanges);
        Assert.NotNull(harness.OnError);
        harness.MockProcessor.Verify(x => x.StartAsync(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_BeforeStart_IsANoOp()
    {
        var harness = new Harness(new BenzeneCosmosChangeFeedConfig());

        await harness.Worker.StopAsync(CancellationToken.None);

        harness.MockProcessor.Verify(x => x.StopAsync(), Times.Never);
    }

    [Fact]
    public async Task StopAsync_AfterStart_StopsTheProcessor()
    {
        var harness = new Harness(new BenzeneCosmosChangeFeedConfig());
        await harness.Worker.StartAsync(CancellationToken.None);

        await harness.Worker.StopAsync(CancellationToken.None);

        harness.MockProcessor.Verify(x => x.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task SuccessfulBatch_HandlerDidNotCheckpoint_AutoCheckpointsByDefault()
    {
        var harness = new Harness(new BenzeneCosmosChangeFeedConfig());
        await harness.Worker.StartAsync(CancellationToken.None);

        await harness.DeliverBatchAsync("order-1", "order-2");

        Assert.Equal(1, harness.CheckpointCalls);
    }

    [Fact]
    public async Task SuccessfulBatch_AutoCheckpointDisabled_DoesNotCheckpoint()
    {
        var harness = new Harness(new BenzeneCosmosChangeFeedConfig { AutoCheckpointOnSuccess = false });
        await harness.Worker.StartAsync(CancellationToken.None);

        await harness.DeliverBatchAsync("order-1");

        Assert.Equal(0, harness.CheckpointCalls);
    }

    [Fact]
    public async Task HandlerCheckpointsItself_NoSecondAutoCheckpoint()
    {
        var harness = new Harness(new BenzeneCosmosChangeFeedConfig());
        harness.MockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<StreamContext<OrderDocument>>(), It.IsAny<IServiceResolver>()))
            .Returns<StreamContext<OrderDocument>, IServiceResolver>(
                (context, _) => context.Checkpointer.CheckpointAsync(new OrderDocument { Id = "order-1" }));
        await harness.Worker.StartAsync(CancellationToken.None);

        await harness.DeliverBatchAsync("order-1");

        Assert.Equal(1, harness.CheckpointCalls);
    }

    [Fact]
    public async Task FailedBatch_Default_RethrowsWithoutCheckpointing_SoTheBatchIsRedelivered()
    {
        var harness = new Harness(new BenzeneCosmosChangeFeedConfig());
        harness.MockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<StreamContext<OrderDocument>>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("handler failed"));
        await harness.Worker.StartAsync(CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.DeliverBatchAsync("order-1"));

        Assert.Equal(0, harness.CheckpointCalls);
    }

    [Fact]
    public async Task FailedBatch_CatchHandlerExceptions_CheckpointsAndContinues_SkippingTheBatch()
    {
        var harness = new Harness(new BenzeneCosmosChangeFeedConfig { CatchHandlerExceptions = true });
        harness.MockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<StreamContext<OrderDocument>>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("handler failed"));
        await harness.Worker.StartAsync(CancellationToken.None);

        await harness.DeliverBatchAsync("order-1");

        Assert.Equal(1, harness.CheckpointCalls);
    }

    [Fact]
    public async Task OnError_LogsAndCompletes()
    {
        var harness = new Harness(new BenzeneCosmosChangeFeedConfig());
        await harness.Worker.StartAsync(CancellationToken.None);

        await harness.OnError("0", new InvalidOperationException("lease failed"));
    }
}
