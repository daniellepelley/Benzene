using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.EventBridge;

/// <summary>
/// Puts events on an EventBridge bus using <c>PutEvents</c> (up to 10 entries per call). Reuses
/// <see cref="EventBridgeContextConverter{T}"/> to build each entry (source/detail-type/detail, with
/// Benzene headers embedded), then reports per-entry failures against the caller's request indices.
/// </summary>
/// <remarks>
/// Unlike SQS/SNS batch, EventBridge <c>PutEvents</c> has no per-entry id: the response
/// <c>Entries</c> list is positional, in the same order as the request, and a failed entry carries an
/// <c>ErrorCode</c>. Failures are therefore mapped back by position within each chunk.
/// </remarks>
public class EventBridgeBatchMessageClient : IBenzeneBatchMessageClient
{
    /// <summary>The maximum number of entries EventBridge accepts in one <c>PutEvents</c> call.</summary>
    public const int MaxBatchSize = 10;

    private readonly IAmazonEventBridge _amazonEventBridge;
    private readonly string _source;
    private readonly string _eventBusName;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBridgeBatchMessageClient"/> class.
    /// </summary>
    /// <param name="source">The event source to publish under.</param>
    /// <param name="amazonEventBridge">The EventBridge client to put events with.</param>
    /// <param name="eventBusName">The target event bus name, or null for the default bus.</param>
    public EventBridgeBatchMessageClient(string source, IAmazonEventBridge amazonEventBridge, string eventBusName = null)
    {
        _source = source;
        _amazonEventBridge = amazonEventBridge;
        _eventBusName = eventBusName;
        _serializer = new JsonSerializer();
    }

    /// <inheritdoc />
    public async Task<BatchSendResult> SendBatchAsync<TRequest>(IReadOnlyCollection<IBenzeneClientRequest<TRequest>> requests)
    {
        var converter = new EventBridgeContextConverter<TRequest>(_source, _eventBusName, _serializer);
        var failures = new List<FailedBatchEntry>();

        foreach (var chunk in BatchSend.Chunk(requests, MaxBatchSize))
        {
            var entries = new List<PutEventsRequestEntry>(chunk.Count);
            foreach (var (request, _) in chunk)
            {
                var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));
                // The converter builds a single-entry request; lift that entry into the batch.
                entries.Add(context.Request.Entries[0]);
            }

            var response = await _amazonEventBridge.PutEventsAsync(new PutEventsRequest
            {
                Entries = entries,
            });

            if (response.FailedEntryCount > 0 && response.Entries != null)
            {
                // PutEvents responds positionally: response entry i corresponds to request entry i.
                for (var i = 0; i < response.Entries.Count && i < chunk.Count; i++)
                {
                    var entry = response.Entries[i];
                    if (!string.IsNullOrEmpty(entry.ErrorCode))
                    {
                        failures.Add(new FailedBatchEntry(chunk[i].Index, entry.ErrorCode, entry.ErrorMessage));
                    }
                }
            }
        }

        return new BatchSendResult(failures);
    }

    /// <summary>Disposes the client. No-op; it holds no disposable resources of its own.</summary>
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
