using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Producer;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.EventHub;

/// <summary>
/// Sends events to an Event Hub using a native <see cref="EventDataBatch"/>: events are packed into
/// size-bounded batches (<c>CreateBatchAsync</c> + <c>TryAdd</c>) and each full batch is sent with a
/// single <c>SendAsync</c> call. Reuses <see cref="EventHubContextConverter{T}"/> to build each event
/// (topic/header event properties and the optional partition key).
/// </summary>
/// <remarks>
/// <para>An Event Hubs batch send is atomic — it either accepts every event or throws — so there is no
/// per-event acknowledgement. Failures are reported at batch granularity: if a <c>SendAsync</c>
/// throws, every event in that batch is reported as failed (with the exception message) against its
/// index in the original request collection.</para>
/// <para>A batch's partition key is fixed when the batch is created, so requests are first grouped by
/// their resolved partition key (from <paramref name="partitionKeyHeader"/>); events sharing a key
/// stay co-located on one partition and in order, exactly as the single-send path guarantees.</para>
/// </remarks>
public class EventHubBatchMessageClient : IBenzeneBatchMessageClient
{
    private readonly EventHubProducerClient _producerClient;
    private readonly string _topicPropertyKey;
    private readonly string _partitionKeyHeader;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubBatchMessageClient"/> class.
    /// </summary>
    /// <param name="producerClient">The Event Hubs producer client to send with.</param>
    /// <param name="topicPropertyKey">The event property the topic is written to (defaults to <see cref="EventHubContextConverter{T}.DefaultTopicProperty"/>).</param>
    /// <param name="partitionKeyHeader">The request header whose value becomes the partition key (defaults to <c>null</c> — no key).</param>
    public EventHubBatchMessageClient(EventHubProducerClient producerClient,
        string topicPropertyKey = EventHubContextConverter<object>.DefaultTopicProperty,
        string partitionKeyHeader = null)
    {
        _producerClient = producerClient;
        _topicPropertyKey = topicPropertyKey;
        _partitionKeyHeader = partitionKeyHeader;
        _serializer = new JsonSerializer();
    }

    /// <inheritdoc />
    public async Task<BatchSendResult> SendBatchAsync<TRequest>(IReadOnlyCollection<IBenzeneClientRequest<TRequest>> requests)
    {
        var converter = new EventHubContextConverter<TRequest>(_serializer, _topicPropertyKey, _partitionKeyHeader);
        var failures = new List<FailedBatchEntry>();

        // A batch is bound to one partition key, so group by the resolved key before batching.
        var groups = new Dictionary<string, List<(EventHubSendMessageContext Context, int Index)>>();
        var index = 0;
        foreach (var request in requests)
        {
            // Build (serialize) each event individually so a single bad entry becomes that entry's
            // failure rather than aborting the whole SendBatchAsync - matching the AWS batch clients'
            // per-entry contract.
            EventHubSendMessageContext context;
            try
            {
                context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));
            }
            catch (System.Exception ex)
            {
                failures.Add(new FailedBatchEntry(index, ex.GetType().Name, ex.Message));
                index++;
                continue;
            }

            var key = context.PartitionKey ?? string.Empty;
            if (!groups.TryGetValue(key, out var group))
            {
                group = new List<(EventHubSendMessageContext, int)>();
                groups[key] = group;
            }

            group.Add((context, index));
            index++;
        }

        foreach (var group in groups)
        {
            await SendGroupAsync(group.Key, group.Value, failures);
        }

        return new BatchSendResult(failures);
    }

    private async Task SendGroupAsync(string partitionKey, List<(EventHubSendMessageContext Context, int Index)> group, List<FailedBatchEntry> failures)
    {
        var batchOptions = string.IsNullOrEmpty(partitionKey)
            ? new CreateBatchOptions()
            : new CreateBatchOptions { PartitionKey = partitionKey };

        // Hold the native EventDataBatch (unmanaged AMQP memory) in a try/finally: TryAdd can throw on a
        // malformed event, and without the finally the batch would leak on that path. On a roll the old
        // batch is disposed and the local nulled before the next is created, so a failed CreateBatchAsync
        // can't leave the finally double-disposing it.
        EventDataBatch? batch = await _producerClient.CreateBatchAsync(batchOptions);
        var batchIndices = new List<int>();

        try
        {
            foreach (var (context, itemIndex) in group)
            {
                if (!batch.TryAdd(context.EventData))
                {
                    if (batchIndices.Count == 0)
                    {
                        failures.Add(new FailedBatchEntry(itemIndex, "EventTooLarge",
                            "The event is too large to fit in a single Event Hubs batch."));
                        continue;
                    }

                    await SendBatchAndTrackFailuresAsync(batch, batchIndices, failures);
                    batch.Dispose();
                    batch = null;

                    batch = await _producerClient.CreateBatchAsync(batchOptions);
                    batchIndices = new List<int>();

                    if (!batch.TryAdd(context.EventData))
                    {
                        failures.Add(new FailedBatchEntry(itemIndex, "EventTooLarge",
                            "The event is too large to fit in a single Event Hubs batch."));
                        continue;
                    }
                }

                batchIndices.Add(itemIndex);
            }

            if (batchIndices.Count > 0)
            {
                await SendBatchAndTrackFailuresAsync(batch, batchIndices, failures);
            }
        }
        finally
        {
            batch?.Dispose();
        }
    }

    private async Task SendBatchAndTrackFailuresAsync(EventDataBatch batch, List<int> batchIndices, List<FailedBatchEntry> failures)
    {
        try
        {
            await _producerClient.SendAsync(batch);
        }
        catch (System.Exception ex)
        {
            foreach (var failedIndex in batchIndices)
            {
                failures.Add(new FailedBatchEntry(failedIndex, ex.GetType().Name, ex.Message));
            }
        }
    }

    /// <summary>Disposes the client. No-op; the caller owns the <see cref="EventHubProducerClient"/>'s lifetime.</summary>
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
