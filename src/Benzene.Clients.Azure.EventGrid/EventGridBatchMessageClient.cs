using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.EventGrid;

/// <summary>
/// Publishes events to an Event Grid topic using <c>SendEventsAsync</c> (one request per chunk of
/// CloudEvents). Reuses <see cref="EventGridContextConverter{T}"/> to build each CloudEvent (topic →
/// <c>Type</c>, headers → lower-cased extension attributes).
/// </summary>
/// <remarks>
/// An Event Grid batch publish is atomic — it either accepts every event in the request or throws — so
/// there is no per-event acknowledgement. Failures are reported at chunk granularity: if a
/// <c>SendEventsAsync</c> throws, every event in that chunk is reported as failed (with the exception
/// message) against its index in the original request collection. Event Grid caps a request at ~1 MB
/// total, so events are chunked to <see cref="MaxBatchSize"/> per request (override via the
/// constructor for larger or smaller payloads).
/// </remarks>
public class EventGridBatchMessageClient : IBenzeneBatchMessageClient
{
    /// <summary>The default number of events published per <c>SendEventsAsync</c> request.</summary>
    public const int MaxBatchSize = 100;

    private readonly EventGridPublisherClient _publisherClient;
    private readonly string _source;
    private readonly int _batchSize;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridBatchMessageClient"/> class.
    /// </summary>
    /// <param name="source">The CloudEvent <c>source</c> — identifies the context the events happened in.</param>
    /// <param name="publisherClient">The Event Grid publisher client to publish with.</param>
    /// <param name="batchSize">The number of events per request (defaults to <see cref="MaxBatchSize"/>); the Event Grid ~1 MB request cap still applies.</param>
    public EventGridBatchMessageClient(string source, EventGridPublisherClient publisherClient, int batchSize = MaxBatchSize)
    {
        _source = source;
        _publisherClient = publisherClient;
        _batchSize = batchSize;
        _serializer = new JsonSerializer();
    }

    /// <inheritdoc />
    public async Task<BatchSendResult> SendBatchAsync<TRequest>(IReadOnlyCollection<IBenzeneClientRequest<TRequest>> requests)
    {
        var converter = new EventGridContextConverter<TRequest>(_source, _serializer);
        var failures = new List<FailedBatchEntry>();

        foreach (var chunk in BatchSend.Chunk(requests, _batchSize))
        {
            var events = new List<CloudEvent>(chunk.Count);
            var sentIndices = new List<int>(chunk.Count);
            foreach (var (request, itemIndex) in chunk)
            {
                // Build (serialize) each event individually so a single bad entry becomes that
                // entry's failure rather than aborting the whole SendBatchAsync after earlier chunks
                // already published - matching the AWS batch clients' per-entry contract.
                try
                {
                    var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));
                    events.Add(context.CloudEvent!);
                    sentIndices.Add(itemIndex);
                }
                catch (System.Exception ex)
                {
                    failures.Add(new FailedBatchEntry(itemIndex, ex.GetType().Name, ex.Message));
                }
            }

            if (events.Count == 0)
            {
                continue;
            }

            try
            {
                await _publisherClient.SendEventsAsync(events);
            }
            catch (System.Exception ex)
            {
                // Atomic send: on a throw every event that actually made it into this request failed
                // (the conversion failures above are already recorded and are not re-counted here).
                foreach (var itemIndex in sentIndices)
                {
                    failures.Add(new FailedBatchEntry(itemIndex, ex.GetType().Name, ex.Message));
                }
            }
        }

        return new BatchSendResult(failures);
    }

    /// <summary>Disposes the client. No-op; the caller owns the <see cref="EventGridPublisherClient"/>'s lifetime.</summary>
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
