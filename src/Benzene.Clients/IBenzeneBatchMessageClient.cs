using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.Messages.BenzeneClient;

namespace Benzene.Clients;

/// <summary>
/// Sends a collection of messages to a transport using the provider's native <em>batch</em> primitive
/// (SNS <c>PublishBatch</c>, SQS <c>SendMessageBatch</c>, EventBridge <c>PutEvents</c>, an Event Hub
/// <c>EventDataBatch</c>, …) instead of one call per message. A separate interface from
/// <see cref="IBenzeneMessageClient"/> so the single-send path is unchanged; a client can implement
/// either or both. Batch sending is fire-and-forget with per-entry acknowledgement — there is no
/// typed response, only which entries failed.
/// </summary>
public interface IBenzeneBatchMessageClient : IDisposable
{
    /// <summary>
    /// Sends every request in <paramref name="requests"/>, chunking to the provider's per-batch limit
    /// and issuing one batch call per chunk.
    /// </summary>
    /// <typeparam name="TRequest">The message payload type.</typeparam>
    /// <param name="requests">The messages to send, in order.</param>
    /// <returns>
    /// A <see cref="BatchSendResult"/> listing the entries that failed (by their index in
    /// <paramref name="requests"/>), so the caller can retry just those. Empty when all succeeded.
    /// </returns>
    Task<BatchSendResult> SendBatchAsync<TRequest>(IReadOnlyCollection<IBenzeneClientRequest<TRequest>> requests);
}
