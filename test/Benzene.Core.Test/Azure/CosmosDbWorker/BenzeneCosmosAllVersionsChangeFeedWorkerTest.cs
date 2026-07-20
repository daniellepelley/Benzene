using System;
using System.Collections.Generic;
using System.Linq;
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

public class BenzeneCosmosAllVersionsChangeFeedWorkerTest
{
    // Public so Moq can proxy ILogger<BenzeneCosmosAllVersionsChangeFeedWorker<OrderDocument>>.
    public class OrderDocument
    {
        public string Id { get; set; }
    }

    private static ChangeFeedItem<OrderDocument> Item(OrderDocument current, OrderDocument previous, ChangeFeedOperationType op)
    {
        var metadata = new ChangeFeedMetadata();
        // OperationType has a non-public setter (it's a deserialization target); set it via reflection.
        typeof(ChangeFeedMetadata).GetProperty(nameof(ChangeFeedMetadata.OperationType))!
            .GetSetMethod(nonPublic: true)!.Invoke(metadata, new object[] { op });
        return new ChangeFeedItem<OrderDocument> { Current = current, Previous = previous, Metadata = metadata };
    }

    private class Harness
    {
        public Mock<IMiddlewarePipeline<StreamContext<CosmosChangeFeedItem<OrderDocument>>>> MockPipeline { get; } = new();
        public Mock<ChangeFeedProcessor> MockProcessor { get; } = new();
        public Mock<ICosmosChangeFeedProcessorFactory<OrderDocument>> MockFactory { get; } = new();
        public BenzeneCosmosAllVersionsChangeFeedWorker<OrderDocument> Worker { get; }
        public Container.ChangeFeedHandler<ChangeFeedItem<OrderDocument>> OnChanges { get; private set; }
        public Container.ChangeFeedMonitorErrorDelegate OnError { get; private set; }

        public Harness(BenzeneCosmosAllVersionsChangeFeedConfig config)
        {
            MockProcessor.Setup(x => x.StartAsync()).Returns(Task.CompletedTask);
            MockProcessor.Setup(x => x.StopAsync()).Returns(Task.CompletedTask);
            MockFactory
                .Setup(x => x.CreateAllVersionsAndDeletes(
                    It.IsAny<Container.ChangeFeedHandler<ChangeFeedItem<OrderDocument>>>(),
                    It.IsAny<Container.ChangeFeedMonitorErrorDelegate>()))
                .Callback<Container.ChangeFeedHandler<ChangeFeedItem<OrderDocument>>, Container.ChangeFeedMonitorErrorDelegate>(
                    (onChanges, onError) =>
                    {
                        OnChanges = onChanges;
                        OnError = onError;
                    })
                .Returns(MockProcessor.Object);

            var mockResolver = new Mock<IServiceResolver>();
            mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
            mockResolver.Setup(x => x.GetService<ILogger<BenzeneCosmosAllVersionsChangeFeedWorker<OrderDocument>>>())
                .Returns(Mock.Of<ILogger<BenzeneCosmosAllVersionsChangeFeedWorker<OrderDocument>>>());
            var mockResolverFactory = new Mock<IServiceResolverFactory>();
            mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);

            var application = new CosmosAllVersionsChangeFeedApplication<OrderDocument>(MockPipeline.Object);
            Worker = new BenzeneCosmosAllVersionsChangeFeedWorker<OrderDocument>(
                mockResolverFactory.Object, application, config, MockFactory.Object);
        }

        public Task DeliverBatchAsync(params ChangeFeedItem<OrderDocument>[] changes)
        {
            var mockContext = new Mock<ChangeFeedProcessorContext>();
            mockContext.Setup(x => x.LeaseToken).Returns("0");
            return OnChanges(mockContext.Object, changes, CancellationToken.None);
        }
    }

    [Fact]
    public void Config_Defaults_CatchHandlerExceptionsFalse()
    {
        Assert.False(new BenzeneCosmosAllVersionsChangeFeedConfig().CatchHandlerExceptions);
    }

    [Fact]
    public async Task StartAsync_CreatesAllVersionsProcessor_AndStartsIt()
    {
        var harness = new Harness(new BenzeneCosmosAllVersionsChangeFeedConfig());

        await harness.Worker.StartAsync(CancellationToken.None);

        harness.MockFactory.Verify(x => x.CreateAllVersionsAndDeletes(
            It.IsAny<Container.ChangeFeedHandler<ChangeFeedItem<OrderDocument>>>(),
            It.IsAny<Container.ChangeFeedMonitorErrorDelegate>()), Times.Once);
        Assert.NotNull(harness.OnChanges);
        harness.MockProcessor.Verify(x => x.StartAsync(), Times.Once);
    }

    [Fact]
    public async Task DeliveredBatch_MapsChangeItems_IncludingDeletesAndPreviousState()
    {
        var harness = new Harness(new BenzeneCosmosAllVersionsChangeFeedConfig());
        StreamContext<CosmosChangeFeedItem<OrderDocument>> captured = null;
        harness.MockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<StreamContext<CosmosChangeFeedItem<OrderDocument>>>(), It.IsAny<IServiceResolver>()))
            .Callback<StreamContext<CosmosChangeFeedItem<OrderDocument>>, IServiceResolver>((ctx, _) => captured = ctx)
            .Returns(Task.CompletedTask);
        await harness.Worker.StartAsync(CancellationToken.None);

        await harness.DeliverBatchAsync(
            Item(new OrderDocument { Id = "o1" }, null, ChangeFeedOperationType.Create),
            Item(new OrderDocument { Id = "o2" }, new OrderDocument { Id = "o2" }, ChangeFeedOperationType.Replace),
            Item(new OrderDocument { Id = "o3" }, new OrderDocument { Id = "o3" }, ChangeFeedOperationType.Delete));

        Assert.NotNull(captured);
        var items = new List<CosmosChangeFeedItem<OrderDocument>>();
        await foreach (var item in captured.Items)
        {
            items.Add(item);
        }

        Assert.Equal(3, items.Count);
        Assert.Equal(CosmosChangeType.Create, items[0].ChangeType);
        Assert.Equal("o1", items[0].Current.Id);
        Assert.Equal(CosmosChangeType.Replace, items[1].ChangeType);
        Assert.Equal(CosmosChangeType.Delete, items[2].ChangeType);
        Assert.Equal("o3", items[2].Previous.Id); // previous state retained on delete
        Assert.Equal("0", captured.Metadata[CosmosAllVersionsChangeFeedApplication<OrderDocument>.LeaseTokenMetadataKey]);
    }

    [Fact]
    public async Task FailedBatch_Default_Rethrows_SoTheProcessorDoesNotCheckpoint()
    {
        var harness = new Harness(new BenzeneCosmosAllVersionsChangeFeedConfig());
        harness.MockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<StreamContext<CosmosChangeFeedItem<OrderDocument>>>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("handler failed"));
        await harness.Worker.StartAsync(CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.DeliverBatchAsync(Item(new OrderDocument { Id = "o1" }, null, ChangeFeedOperationType.Create)));
    }

    [Fact]
    public async Task FailedBatch_CatchHandlerExceptions_SwallowsSoTheProcessorCheckpoints()
    {
        var harness = new Harness(new BenzeneCosmosAllVersionsChangeFeedConfig { CatchHandlerExceptions = true });
        harness.MockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<StreamContext<CosmosChangeFeedItem<OrderDocument>>>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("handler failed"));
        await harness.Worker.StartAsync(CancellationToken.None);

        // No throw: swallowing lets the automatic checkpoint advance past the poison batch.
        await harness.DeliverBatchAsync(Item(new OrderDocument { Id = "o1" }, null, ChangeFeedOperationType.Create));
    }

    [Fact]
    public async Task SkipMode_ShutdownCancellation_Propagates_SoTheBatchIsNotCheckpointed()
    {
        // Even with CatchHandlerExceptions=true (skip mode), a genuine shutdown cancellation must
        // propagate so the automatic-checkpoint processor doesn't checkpoint a partial batch.
        var harness = new Harness(new BenzeneCosmosAllVersionsChangeFeedConfig { CatchHandlerExceptions = true });
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        harness.MockPipeline
            .Setup(x => x.HandleAsync(It.IsAny<StreamContext<CosmosChangeFeedItem<OrderDocument>>>(), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));
        await harness.Worker.StartAsync(CancellationToken.None);

        var mockContext = new Mock<ChangeFeedProcessorContext>();
        mockContext.Setup(x => x.LeaseToken).Returns("0");

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            harness.OnChanges(mockContext.Object,
                new[] { Item(new OrderDocument { Id = "o1" }, null, ChangeFeedOperationType.Create) }, cts.Token));
    }

    [Fact]
    public async Task StopAsync_AfterStart_StopsTheProcessor()
    {
        var harness = new Harness(new BenzeneCosmosAllVersionsChangeFeedConfig());
        await harness.Worker.StartAsync(CancellationToken.None);

        await harness.Worker.StopAsync(CancellationToken.None);

        harness.MockProcessor.Verify(x => x.StopAsync(), Times.Once);
    }
}
