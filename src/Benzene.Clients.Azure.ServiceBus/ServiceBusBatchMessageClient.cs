using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Clients.Common;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.ServiceBus;

/// <summary>
/// Sends messages to a Service Bus queue or topic using a native <see cref="ServiceBusMessageBatch"/>:
/// messages are packed into size-bounded batches (<c>CreateMessageBatchAsync</c> + <c>TryAdd</c>) and
/// each full batch is sent with a single <c>SendMessagesAsync</c> call. Reuses
/// <see cref="ServiceBusContextConverter{T}"/> to build each message (topic/header application
/// properties and any broker-level sender properties).
/// </summary>
/// <remarks>
/// Unlike SQS/SNS, a Service Bus batch send is atomic — it either accepts every message in the batch
/// or throws — so there is no per-message acknowledgement. Failures are therefore reported at batch
/// granularity: if a <c>SendMessagesAsync</c> throws, every message in that batch is reported as
/// failed (with the exception message), against its index in the original request collection. A single
/// message too large to fit an empty batch is reported as its own failure without aborting the rest.
/// </remarks>
public class ServiceBusBatchMessageClient : IBenzeneBatchMessageClient
{
    private readonly ServiceBusSender _sender;
    private readonly string _topicPropertyKey;
    private readonly ServiceBusSenderProperties? _senderProperties;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusBatchMessageClient"/> class.
    /// </summary>
    /// <param name="sender">The Service Bus sender (bound to a queue or topic) to send with.</param>
    /// <param name="topicPropertyKey">The application property the topic is written to (defaults to <see cref="ServiceBusContextConverter{T}.DefaultTopicProperty"/>).</param>
    /// <param name="senderProperties">Optional mapping of headers onto broker-level message properties (MessageId, SessionId, ScheduledEnqueueTime, TimeToLive).</param>
    public ServiceBusBatchMessageClient(ServiceBusSender sender,
        string topicPropertyKey = ServiceBusContextConverter<object>.DefaultTopicProperty,
        ServiceBusSenderProperties? senderProperties = null)
    {
        _sender = sender;
        _topicPropertyKey = topicPropertyKey;
        _senderProperties = senderProperties;
        _serializer = new JsonSerializer();
    }

    /// <inheritdoc />
    public async Task<BatchSendResult> SendBatchAsync<TRequest>(IReadOnlyCollection<IBenzeneClientRequest<TRequest>> requests)
    {
        var converter = new ServiceBusContextConverter<TRequest>(_serializer, _topicPropertyKey, _senderProperties);
        var failures = new List<FailedBatchEntry>();

        // Hold the native batch (unmanaged AMQP memory) in a try/finally: converter.CreateRequestAsync
        // (serialization) or TryAddMessage can throw mid-loop, and without the finally the batch would
        // leak on that path. On a roll, the old batch is disposed and the local nulled before creating
        // the next, so a failed CreateMessageBatchAsync can't leave the finally double-disposing it.
        ServiceBusMessageBatch? batch = await _sender.CreateMessageBatchAsync();
        var batchIndices = new List<int>();
        var index = 0;

        try
        {
            foreach (var request in requests)
            {
                // Build (serialize) each message individually so a single bad entry becomes that
                // entry's failure rather than aborting the whole SendBatchAsync after earlier full
                // batches already sent - matching the AWS batch clients' per-entry contract.
                ServiceBusMessage message;
                try
                {
                    var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));
                    message = context.Message;
                }
                catch (System.Exception ex)
                {
                    failures.Add(new FailedBatchEntry(index, ex.GetType().Name, ex.Message));
                    index++;
                    continue;
                }

                if (!batch.TryAddMessage(message))
                {
                    if (batchIndices.Count == 0)
                    {
                        // The message can't fit even an empty batch: it is individually too large.
                        failures.Add(new FailedBatchEntry(index, "MessageTooLarge",
                            "The message is too large to fit in a single Service Bus batch."));
                        index++;
                        continue;
                    }

                    // The current batch is full: flush it, then start a fresh batch for this message.
                    await SendBatchAndTrackFailuresAsync(batch, batchIndices, failures);
                    batch.Dispose();
                    batch = null;

                    batch = await _sender.CreateMessageBatchAsync();
                    batchIndices = new List<int>();

                    if (!batch.TryAddMessage(message))
                    {
                        failures.Add(new FailedBatchEntry(index, "MessageTooLarge",
                            "The message is too large to fit in a single Service Bus batch."));
                        index++;
                        continue;
                    }
                }

                batchIndices.Add(index);
                index++;
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

        return new BatchSendResult(failures);
    }

    private async Task SendBatchAndTrackFailuresAsync(ServiceBusMessageBatch batch, List<int> batchIndices, List<FailedBatchEntry> failures)
    {
        try
        {
            await _sender.SendMessagesAsync(batch);
        }
        catch (System.Exception ex)
        {
            foreach (var failedIndex in batchIndices)
            {
                failures.Add(new FailedBatchEntry(failedIndex, ex.GetType().Name, ex.Message));
            }
        }
    }

    /// <summary>Disposes the client. No-op; the caller owns the <see cref="ServiceBusSender"/>'s lifetime.</summary>
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
