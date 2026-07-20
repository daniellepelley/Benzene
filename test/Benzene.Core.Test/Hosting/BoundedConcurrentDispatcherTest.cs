using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benzene.SelfHost;
using Benzene.Test.Logging.Helpers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Benzene.Test.Hosting;

public class BoundedConcurrentDispatcherTest
{
    private static FakeLogger<BoundedConcurrentDispatcherTest> CreateLogger(out FakeLogCollector collector)
    {
        collector = new FakeLogCollector();
        return new FakeLogger<BoundedConcurrentDispatcherTest>(collector);
    }

    [Fact]
    public async Task EnqueueAsync_RoundRobin_NeverRunsMoreThanLaneCountConcurrently()
    {
        var logger = CreateLogger(out _);
        var concurrent = 0;
        var maxObserved = 0;
        var gate = new object();
        var completions = new ConcurrentBag<TaskCompletionSource>();

        var dispatcher = new BoundedConcurrentDispatcher<int>(laneCount: 2, async (item, ct) =>
        {
            lock (gate)
            {
                concurrent++;
                maxObserved = Math.Max(maxObserved, concurrent);
            }

            await Task.Delay(50, ct);

            lock (gate)
            {
                concurrent--;
            }
        }, logger);

        for (var i = 0; i < 6; i++)
        {
            var tcs = new TaskCompletionSource();
            completions.Add(tcs);
            await dispatcher.EnqueueAsync(i, CancellationToken.None);
        }

        await dispatcher.DrainAsync(TimeSpan.FromSeconds(5));

        Assert.True(maxObserved >= 2, $"Expected at least 2 concurrent handlers, observed {maxObserved}.");
        Assert.True(maxObserved <= 2, $"Expected at most 2 concurrent handlers (laneCount), observed {maxObserved}.");
    }

    [Fact]
    public async Task EnqueueAsync_ItemsSharingAKey_CompleteInEnqueueOrder()
    {
        var logger = CreateLogger(out _);
        var completionOrder = new ConcurrentQueue<string>();

        var dispatcher = new BoundedConcurrentDispatcher<(int Key, string Value, int DelayMs)>(
            laneCount: 3,
            async (item, ct) =>
            {
                await Task.Delay(item.DelayMs, ct);
                completionOrder.Enqueue(item.Value);
            },
            logger,
            keySelector: item => item.Key);

        // Same key (1): decreasing delays, so a naive unordered-parallel dispatch would complete
        // C before B before A. A key-ordered single-consumer lane must still complete A, B, C in
        // enqueue order regardless.
        await dispatcher.EnqueueAsync((1, "A", 60), CancellationToken.None);
        await dispatcher.EnqueueAsync((1, "B", 30), CancellationToken.None);
        await dispatcher.EnqueueAsync((1, "C", 10), CancellationToken.None);

        await dispatcher.DrainAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(new[] { "A", "B", "C" }, completionOrder.ToArray());
    }

    [Fact]
    public async Task EnqueueAsync_DifferentKeys_RunConcurrentlyOnDifferentLanes()
    {
        var logger = CreateLogger(out _);
        var concurrent = 0;
        var maxObserved = 0;
        var gate = new object();

        var dispatcher = new BoundedConcurrentDispatcher<int>(laneCount: 4, async (item, ct) =>
        {
            lock (gate)
            {
                concurrent++;
                maxObserved = Math.Max(maxObserved, concurrent);
            }

            await Task.Delay(50, ct);

            lock (gate)
            {
                concurrent--;
            }
        }, logger, keySelector: item => item);

        for (var i = 0; i < 4; i++)
        {
            await dispatcher.EnqueueAsync(i, CancellationToken.None);
        }

        await dispatcher.DrainAsync(TimeSpan.FromSeconds(5));

        Assert.True(maxObserved > 1, $"Expected different keys to run concurrently, observed max {maxObserved}.");
    }

    [Fact]
    public async Task Dispatch_ExceptionInOneItem_IsLoggedAndDoesNotStopTheLane()
    {
        var logger = CreateLogger(out var collector);
        var secondItemRan = false;

        var dispatcher = new BoundedConcurrentDispatcher<int>(laneCount: 1, (item, ct) =>
        {
            if (item == 1)
            {
                throw new InvalidOperationException("boom");
            }

            secondItemRan = true;
            return Task.CompletedTask;
        }, logger);

        await dispatcher.EnqueueAsync(1, CancellationToken.None);
        await dispatcher.EnqueueAsync(2, CancellationToken.None);

        await dispatcher.DrainAsync(TimeSpan.FromSeconds(5));

        Assert.True(secondItemRan, "The lane should keep processing after a fault in an earlier item.");
        Assert.Contains(collector.Entries, e => e.Level == LogLevel.Error && e.Exception is InvalidOperationException);
    }

    [Fact]
    public async Task Dispatch_CatchExceptionsFalse_ExceptionPropagatesAndInvokesOnFault_LaneStopsConsuming()
    {
        var logger = CreateLogger(out var collector);
        var secondItemRan = false;
        Exception faultSeen = null;

        var dispatcher = new BoundedConcurrentDispatcher<int>(laneCount: 1, (item, ct) =>
        {
            if (item == 1)
            {
                throw new InvalidOperationException("boom");
            }

            secondItemRan = true;
            return Task.CompletedTask;
        }, logger, catchExceptions: false, onFault: ex => faultSeen = ex);

        await dispatcher.EnqueueAsync(1, CancellationToken.None);
        await dispatcher.EnqueueAsync(2, CancellationToken.None);

        await dispatcher.DrainAsync(TimeSpan.FromSeconds(5));

        Assert.False(secondItemRan, "The lane should have stopped consuming after the fault.");
        Assert.IsType<InvalidOperationException>(faultSeen);
        Assert.Contains(collector.Entries, e => e.Level == LogLevel.Error && e.Exception is InvalidOperationException);
    }

    [Fact]
    public async Task DrainAsync_WaitsForInFlightWorkToFinish()
    {
        var logger = CreateLogger(out _);
        var completed = false;

        var dispatcher = new BoundedConcurrentDispatcher<int>(laneCount: 1, async (item, ct) =>
        {
            await Task.Delay(200, ct);
            completed = true;
        }, logger);

        await dispatcher.EnqueueAsync(1, CancellationToken.None);

        await dispatcher.DrainAsync(TimeSpan.FromSeconds(5));

        Assert.True(completed, "DrainAsync should wait for in-flight work to finish before returning.");
    }

    [Fact]
    public async Task DrainAsync_AbandonsInFlightWorkOnceTheTimeoutElapses()
    {
        var logger = CreateLogger(out _);
        var neverCompletes = new TaskCompletionSource();

        var dispatcher = new BoundedConcurrentDispatcher<int>(laneCount: 1, (item, ct) => neverCompletes.Task, logger);

        await dispatcher.EnqueueAsync(1, CancellationToken.None);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await dispatcher.DrainAsync(TimeSpan.FromMilliseconds(100));
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 2000, $"DrainAsync should return once its timeout elapses, took {sw.ElapsedMilliseconds}ms.");
    }

    [Fact]
    public async Task DrainLanesAsync_WaitsForOnlyTheTargetedLanesInFlightWork()
    {
        var logger = CreateLogger(out _);
        var release = new TaskCompletionSource();
        var lane0Completed = false;

        // laneCount 2, key selector = the key: key 0 -> lane 0, key 1 -> lane 1.
        var dispatcher = new BoundedConcurrentDispatcher<int>(laneCount: 2, async (item, ct) =>
        {
            if (item == 0)
            {
                await release.Task;
                lane0Completed = true;
            }
        }, logger, keySelector: item => item);

        await dispatcher.EnqueueAsync(0, CancellationToken.None); // lane 0, blocks until released
        await dispatcher.EnqueueAsync(1, CancellationToken.None); // lane 1, completes immediately

        // Draining only lane 1's key returns promptly even though lane 0 is still in flight.
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await dispatcher.DrainLanesAsync(new[] { 1 }, TimeSpan.FromSeconds(5));
        sw.Stop();

        Assert.False(lane0Completed, "Lane 0 should still be in flight - it was not targeted.");
        Assert.True(sw.ElapsedMilliseconds < 2000, $"Draining an idle lane should return promptly, took {sw.ElapsedMilliseconds}ms.");

        release.SetResult();
        await dispatcher.DrainAsync(TimeSpan.FromSeconds(5));
        Assert.True(lane0Completed);
    }

    [Fact]
    public async Task DrainLanesAsync_ReturnsOnceTheTargetedLaneQuiesces()
    {
        var logger = CreateLogger(out _);
        var completed = false;

        var dispatcher = new BoundedConcurrentDispatcher<int>(laneCount: 2, async (item, ct) =>
        {
            await Task.Delay(150, ct);
            completed = true;
        }, logger, keySelector: item => item);

        await dispatcher.EnqueueAsync(0, CancellationToken.None);

        await dispatcher.DrainLanesAsync(new[] { 0 }, TimeSpan.FromSeconds(5));

        Assert.True(completed, "DrainLanesAsync should wait for the targeted lane's in-flight work to finish.");
    }

    [Fact]
    public async Task DrainLanesAsync_ReturnsOnTimeoutWhenWorkNeverFinishes()
    {
        var logger = CreateLogger(out _);
        var neverCompletes = new TaskCompletionSource();

        var dispatcher = new BoundedConcurrentDispatcher<int>(laneCount: 2, (item, ct) => neverCompletes.Task, logger,
            keySelector: item => item);

        await dispatcher.EnqueueAsync(0, CancellationToken.None);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await dispatcher.DrainLanesAsync(new[] { 0 }, TimeSpan.FromMilliseconds(100));
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 2000, $"DrainLanesAsync should return once its timeout elapses, took {sw.ElapsedMilliseconds}ms.");
    }
}
