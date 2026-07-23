using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Benzene.SelfHost;

/// <summary>
/// Dispatches items pulled from a self-hosted worker's poll loop (e.g.
/// <see cref="Benzene.Kafka.Core.BenzeneKafkaWorker{TKey,TValue}"/>) to an async handler, bounding how many
/// handlers run at once. Built on <see cref="System.Threading.Channels"/> - no new NuGet dependency,
/// part of the BCL.
/// </summary>
/// <remarks>
/// Runs <paramref name="laneCount"/> independent lanes, each a single-consumer <see cref="Channel{T}"/>
/// with one dedicated consumer <see cref="Task"/>. When <c>keySelector</c> is supplied, items
/// sharing a key always route to the same lane, so that lane's strictly-FIFO consumer preserves
/// order for that key (e.g. a Kafka partition) while different keys still run concurrently, up to
/// <paramref name="laneCount"/> at once. With no <c>keySelector</c>, items round-robin across lanes
/// with no ordering promise.
///
/// Each lane's channel has a bounded capacity of 1, so <see cref="EnqueueAsync"/> blocks once a lane
/// already has one item queued behind the one it's actively processing - this is what gives the
/// poll loop calling <see cref="EnqueueAsync"/> real backpressure (the same role the semaphore played
/// in the pattern this replaces), rather than letting an unbounded in-memory backlog build up when
/// handlers fall behind the arrival rate.
///
/// A fault thrown by <c>handle</c> is always logged per item; by default (<c>catchExceptions</c>
/// <c>true</c>) it's then swallowed - it never stops that lane's consumer loop or goes unobserved,
/// unlike the fire-and-forget <c>.ContinueWith(...)</c> pattern this replaces. With
/// <c>catchExceptions</c> <c>false</c>, the fault is rethrown after logging (and after invoking
/// <c>onFault</c>, if supplied) - this ends that lane's consume loop. Since a dead lane's channel
/// still has capacity 1 and nothing left to read it, callers that route by key (e.g. Kafka
/// partition) will eventually block in <see cref="EnqueueAsync"/> once that lane's channel fills -
/// <c>onFault</c> exists so a caller can react (e.g. stop the whole worker) rather than silently
/// deadlock.
/// </remarks>
/// <typeparam name="T">The item type pulled from the poll loop.</typeparam>
public sealed class BoundedConcurrentDispatcher<T>
{
    private readonly Channel<T>[] _lanes;
    private readonly Task[] _consumers;
    private readonly Func<T, int>? _keySelector;
    private readonly int[] _laneOutstanding;
    private int _roundRobinCounter = -1;

    /// <summary>Initializes a new instance of the <see cref="BoundedConcurrentDispatcher{T}"/> class.</summary>
    /// <param name="laneCount">The maximum number of items handled concurrently.</param>
    /// <param name="handle">The async handler each dispatched item is passed to.</param>
    /// <param name="logger">Logs a fault from <paramref name="handle"/>, regardless of <paramref name="catchExceptions"/>.</param>
    /// <param name="keySelector">
    /// When provided, routes items sharing the same key to the same lane, preserving per-key order.
    /// When <c>null</c> (the default), items round-robin across lanes with no ordering guarantee.
    /// </param>
    /// <param name="catchExceptions">
    /// When <c>true</c> (the default), a fault from <paramref name="handle"/> is logged and
    /// swallowed - that lane keeps consuming. When <c>false</c>, the fault is logged, passed to
    /// <paramref name="onFault"/> if supplied, and rethrown - ending that lane's consume loop.
    /// </param>
    /// <param name="onFault">
    /// Invoked with the fault when <paramref name="catchExceptions"/> is <c>false</c> and
    /// <paramref name="handle"/> throws, before the fault is rethrown. Lets a caller react to a
    /// lane dying (e.g. stop the whole worker) instead of leaving it silently one lane short.
    /// </param>
    public BoundedConcurrentDispatcher(int laneCount, Func<T, CancellationToken, Task> handle, ILogger logger,
        Func<T, int>? keySelector = null, bool catchExceptions = true, Action<Exception>? onFault = null)
    {
        if (laneCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(laneCount), laneCount, "Must be at least 1.");
        }

        _keySelector = keySelector;
        _lanes = new Channel<T>[laneCount];
        _consumers = new Task[laneCount];
        _laneOutstanding = new int[laneCount];

        for (var i = 0; i < laneCount; i++)
        {
            _lanes[i] = Channel.CreateBounded<T>(new BoundedChannelOptions(1)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        }

        for (var i = 0; i < laneCount; i++)
        {
            _consumers[i] = ConsumeLoopAsync(i, handle, logger, catchExceptions, onFault);
        }
    }

    /// <summary>Gets the number of lanes items are dispatched across.</summary>
    public int LaneCount => _lanes.Length;

    /// <summary>Maps a key to the lane index it routes to (the same mapping <see cref="EnqueueAsync"/> uses).</summary>
    public int LaneForKey(int key) => (int)((uint)key % (uint)_lanes.Length);

    /// <summary>
    /// Routes <paramref name="item"/> to a lane (by key, if a key selector was supplied, otherwise
    /// round-robin) and enqueues it for that lane's consumer to dispatch.
    /// </summary>
    /// <param name="item">The item to dispatch.</param>
    /// <param name="cancellationToken">Cancels the enqueue.</param>
    public async ValueTask EnqueueAsync(T item, CancellationToken cancellationToken)
    {
        // Both paths reduce through uint: the round-robin counter increments without bound and would,
        // after int.MaxValue enqueues on one dispatcher, wrap to int.MinValue - whose signed modulo is
        // negative and would index _lanes[-x] (IndexOutOfRangeException). Casting to uint first keeps
        // the index in [0, laneCount) across the wrap.
        var laneIndex = _keySelector != null
            ? (int)((uint)_keySelector(item) % (uint)_lanes.Length)
            : (int)((uint)Interlocked.Increment(ref _roundRobinCounter) % (uint)_lanes.Length);

        // Count the item as outstanding for its lane from the moment it's accepted (queued or in
        // flight) until its handler finishes - this is what DrainLanesAsync waits on. If the write is
        // cancelled/faulted, it never entered the lane, so undo the increment.
        Interlocked.Increment(ref _laneOutstanding[laneIndex]);
        try
        {
            await _lanes[laneIndex].Writer.WriteAsync(item, cancellationToken);
        }
        catch
        {
            Interlocked.Decrement(ref _laneOutstanding[laneIndex]);
            throw;
        }
    }

    /// <summary>
    /// Waits for the lanes the given keys route to (via <see cref="LaneForKey"/>) to have no items
    /// queued or in flight, up to <paramref name="timeout"/> - without completing them, so those lanes
    /// keep consuming afterwards. Used on a Kafka consumer-group rebalance to quiesce the revoked
    /// partitions' lanes before their offsets are committed, so no record is committed as done while
    /// still being handled. Because lanes are shared (<c>partition % laneCount</c>), this may also
    /// wait on unrelated keys that share a lane - safe, just conservative.
    /// </summary>
    /// <param name="laneKeys">The keys (e.g. Kafka partition numbers) whose lanes should quiesce.</param>
    /// <param name="timeout">The maximum time to wait before returning even if work is still in flight.</param>
    public async Task DrainLanesAsync(IEnumerable<int> laneKeys, TimeSpan timeout)
    {
        var targetLanes = laneKeys.Select(LaneForKey).Distinct().ToArray();
        if (targetLanes.Length == 0)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        while (targetLanes.Any(i => Volatile.Read(ref _laneOutstanding[i]) > 0))
        {
            if (stopwatch.Elapsed >= timeout)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }
    }

    /// <summary>
    /// Stops accepting new items on every lane and waits for all in-flight handler calls to finish,
    /// up to <paramref name="drainTimeout"/>. Lanes that haven't finished by the timeout are abandoned,
    /// not cancelled - <c>handle</c> is responsible for honoring its own cancellation token, if any.
    /// </summary>
    /// <param name="drainTimeout">The maximum time to wait for in-flight work to finish.</param>
    public async Task DrainAsync(TimeSpan drainTimeout)
    {
        foreach (var lane in _lanes)
        {
            lane.Writer.Complete();
        }

        await Task.WhenAny(Task.WhenAll(_consumers), Task.Delay(drainTimeout));
    }

    private async Task ConsumeLoopAsync(int laneIndex, Func<T, CancellationToken, Task> handle, ILogger logger,
        bool catchExceptions, Action<Exception>? onFault)
    {
        try
        {
            await ConsumeLaneAsync(laneIndex, handle, logger, catchExceptions, onFault);
        }
        finally
        {
            // The lane consumer is exiting - either normal completion (DrainAsync completed the writer
            // and the channel drained, so the count is already 0) or a rethrown fault with
            // catchExceptions=false. On the fault path any item still queued in this lane's bounded
            // channel was counted at enqueue and will never be read now (the consumer is dead), so
            // clear the phantom count - otherwise a later DrainLanesAsync on this lane would see a
            // permanently-nonzero outstanding count and burn its full timeout every time.
            Volatile.Write(ref _laneOutstanding[laneIndex], 0);
        }
    }

    private async Task ConsumeLaneAsync(int laneIndex, Func<T, CancellationToken, Task> handle, ILogger logger,
        bool catchExceptions, Action<Exception>? onFault)
    {
        await foreach (var item in _lanes[laneIndex].Reader.ReadAllAsync())
        {
            try
            {
                await handle(item, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception processing item in Benzene worker loop");

                if (!catchExceptions)
                {
                    onFault?.Invoke(ex);
                    throw;
                }
            }
            finally
            {
                // Runs on every path - success, swallowed exception, and the rethrow above (a finally
                // still runs before the exception propagates) - so each item is un-counted exactly once
                // and a quiescing DrainLanesAsync never hangs on a lane whose consumer is dying.
                Interlocked.Decrement(ref _laneOutstanding[laneIndex]);
            }
        }
    }
}
