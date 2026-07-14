using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Middleware.Streaming;

public class StreamMiddlewareApplicationTest
{
    [Fact]
    public async Task Stream_ReceivesWholeBatch_AsOneOrderedStream_InASingleRun()
    {
        var collected = new List<int>();
        var runs = 0;

        var services = new ServiceCollection();
        var pipeline = new MiddlewarePipelineBuilder<StreamContext<int>>(new MicrosoftBenzeneServiceContainer(services))
            .UseStream<int>(async context =>
            {
                runs++;
                await foreach (var item in context.Items)
                {
                    collected.Add(item);
                }
            })
            .Build();

        var app = new StreamMiddlewareApplication<int[], int>(pipeline,
            batch => new StreamContext<int>(ToAsyncEnumerable(batch)));

        await app.HandleAsync(new[] { 1, 2, 3, 4, 5 }, new MicrosoftServiceResolverFactory(services));

        // Fan-in: the pipeline ran once for the whole batch, not once per item.
        Assert.Equal(1, runs);
        // And the step saw every item, in order.
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, collected);
    }

    [Fact]
    public async Task UseStream_ItemsAndCancellationOverload_ReceivesTheItems()
    {
        var collected = new List<int>();

        var services = new ServiceCollection();
        var pipeline = new MiddlewarePipelineBuilder<StreamContext<int>>(new MicrosoftBenzeneServiceContainer(services))
            .UseStream<int>(async (items, _) =>
            {
                await foreach (var item in items)
                {
                    collected.Add(item);
                }
            })
            .Build();

        var app = new StreamMiddlewareApplication<int[], int>(pipeline,
            batch => new StreamContext<int>(ToAsyncEnumerable(batch)));

        await app.HandleAsync(new[] { 10, 20, 30 }, new MicrosoftServiceResolverFactory(services));

        Assert.Equal(new[] { 10, 20, 30 }, collected);
    }

    [Fact]
    public async Task NullStreamCheckpointer_IsTheDefault_AndNoOps()
    {
        var context = new StreamContext<int>(ToAsyncEnumerable(new[] { 1 }));

        Assert.IsType<NullStreamCheckpointer<int>>(context.Checkpointer);
        await context.Checkpointer.CheckpointAsync(1);   // does not throw
    }

    private static async IAsyncEnumerable<int> ToAsyncEnumerable(int[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }
}
