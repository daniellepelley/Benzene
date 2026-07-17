using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Benzene.SelfHost;

/// <summary>
/// Dispatches items pulled from a self-hosted worker's poll loop (<see cref="Benzene.Kafka.Core.BenzeneKafkaWorker{TKey,TValue}"/>,
/// <see cref="Benzene.SelfHost.Http.BenzeneHttpWorker"/>) to an async handler, bounding how many
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
            _consumers[i] = ConsumeLoopAsync(_lanes[i], handle, logger, catchExceptions, onFault);
        }
    }

    /// <summary>
    /// Routes <paramref name="item"/> to a lane (by key, if a key selector was supplied, otherwise
    /// round-robin) and enqueues it for that lane's consumer to dispatch.
    /// </summary>
    /// <param name="item">The item to dispatch.</param>
    /// <param name="cancellationToken">Cancels the enqueue.</param>
    public ValueTask EnqueueAsync(T item, CancellationToken cancellationToken)
    {
        var laneIndex = _keySelector != null
            ? (int)((uint)_keySelector(item) % _lanes.Length)
            : Interlocked.Increment(ref _roundRobinCounter) % _lanes.Length;

        return _lanes[laneIndex].Writer.WriteAsync(item, cancellationToken);
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

    private static async Task ConsumeLoopAsync(Channel<T> lane, Func<T, CancellationToken, Task> handle, ILogger logger,
        bool catchExceptions, Action<Exception>? onFault)
    {
        await foreach (var item in lane.Reader.ReadAllAsync())
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
        }
    }
}
