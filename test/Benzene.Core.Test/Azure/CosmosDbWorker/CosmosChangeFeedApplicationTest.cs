using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Azure.CosmosDb;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Azure.CosmosDbWorker;

public class CosmosChangeFeedApplicationTest
{
    private class OrderDocument
    {
        public string Id { get; set; }
    }

    private static CosmosChangeFeedBatch<OrderDocument> CreateBatch(
        Func<Task> checkpointAsync = null,
        string leaseToken = "0",
        CancellationToken cancellationToken = default,
        params string[] ids)
    {
        var documents = new List<OrderDocument>();
        foreach (var id in ids)
        {
            documents.Add(new OrderDocument { Id = id });
        }

        return new CosmosChangeFeedBatch<OrderDocument>(documents,
            checkpointAsync ?? (() => Task.CompletedTask), leaseToken, cancellationToken);
    }

    private static IServiceCollection CreateServices()
    {
        return new ServiceCollection().AddSingleton(Mock.Of<ISetCurrentTransport>());
    }

    [Fact]
    public async Task Batch_IsDeliveredAsOneOrderedStream_InASingleRun_NotCheckpointedByDefault()
    {
        var collected = new List<string>();
        var runs = 0;
        var checkpointCalls = 0;

        var services = CreateServices();
        var pipeline = new MiddlewarePipelineBuilder<StreamContext<OrderDocument>>(new MicrosoftBenzeneServiceContainer(services))
            .UseStream<OrderDocument>(async (documents, _) =>
            {
                runs++;
                await foreach (var document in documents)
                {
                    collected.Add(document.Id);
                }
            })
            .Build();
        var application = new CosmosChangeFeedApplication<OrderDocument>(pipeline);

        var batch = CreateBatch(() => { checkpointCalls++; return Task.CompletedTask; },
            ids: new[] { "order-1", "order-2", "order-3" });
        var handlerCheckpointed = await application.HandleAsync(batch, new MicrosoftServiceResolverFactory(services));

        Assert.Equal(1, runs);
        Assert.Equal(new[] { "order-1", "order-2", "order-3" }, collected);
        // The application itself never checkpoints - that's the worker's (or the handler's) call.
        Assert.False(handlerCheckpointed);
        Assert.Equal(0, checkpointCalls);
    }

    [Fact]
    public async Task HandlerCheckpoints_InvokesTheBatchCheckpointHook_AndReportsIt()
    {
        var checkpointCalls = 0;

        var services = CreateServices();
        var pipeline = new MiddlewarePipelineBuilder<StreamContext<OrderDocument>>(new MicrosoftBenzeneServiceContainer(services))
            .UseStream<OrderDocument>(async context =>
            {
                await foreach (var document in context.Items)
                {
                    // Batch-level: the item passed is ignored, the whole batch is acknowledged.
                    await context.Checkpointer.CheckpointAsync(document);
                }
            })
            .Build();
        var application = new CosmosChangeFeedApplication<OrderDocument>(pipeline);

        var batch = CreateBatch(() => { checkpointCalls++; return Task.CompletedTask; }, ids: new[] { "order-1" });
        var handlerCheckpointed = await application.HandleAsync(batch, new MicrosoftServiceResolverFactory(services));

        Assert.True(handlerCheckpointed);
        Assert.Equal(1, checkpointCalls);
    }

    [Fact]
    public async Task Context_CarriesLeaseTokenMetadata_AndCancellationToken()
    {
        StreamContext<OrderDocument> observed = null;

        var services = CreateServices();
        var pipeline = new MiddlewarePipelineBuilder<StreamContext<OrderDocument>>(new MicrosoftBenzeneServiceContainer(services))
            .UseStream<OrderDocument>(context =>
            {
                observed = context;
                return Task.CompletedTask;
            })
            .Build();
        var application = new CosmosChangeFeedApplication<OrderDocument>(pipeline);

        using var cancellationSource = new CancellationTokenSource();
        var batch = CreateBatch(leaseToken: "some-lease", cancellationToken: cancellationSource.Token, ids: new[] { "order-1" });
        await application.HandleAsync(batch, new MicrosoftServiceResolverFactory(services));

        Assert.NotNull(observed);
        Assert.Equal("some-lease", observed.Metadata[CosmosChangeFeedApplication<OrderDocument>.LeaseTokenMetadataKey]);
        Assert.Equal(cancellationSource.Token, observed.CancellationToken);
    }

    [Fact]
    public async Task PipelineThrows_ExceptionPropagates()
    {
        var services = CreateServices();
        var pipeline = new MiddlewarePipelineBuilder<StreamContext<OrderDocument>>(new MicrosoftBenzeneServiceContainer(services))
            .UseStream<OrderDocument>(_ => throw new InvalidOperationException("handler failed"))
            .Build();
        var application = new CosmosChangeFeedApplication<OrderDocument>(pipeline);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            application.HandleAsync(CreateBatch(ids: new[] { "order-1" }), new MicrosoftServiceResolverFactory(services)));
    }
}
