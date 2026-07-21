using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Clients.Common;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.Sqs;

/// <summary>
/// Sends messages to an SQS queue using <c>SendMessageBatch</c> (up to 10 messages per call). Reuses
/// <see cref="SqsContextConverter{T}"/> to build each entry (body + the routing topic / header
/// attributes), then reports per-entry failures against the caller's request indices.
/// </summary>
public class SqsBatchMessageClient : IBenzeneBatchMessageClient
{
    /// <summary>The maximum number of messages SQS accepts in one <c>SendMessageBatch</c> call.</summary>
    public const int MaxBatchSize = 10;

    private readonly IAmazonSQS _amazonSqs;
    private readonly string _queueUrl;
    private readonly string _topicAttributeKey;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsBatchMessageClient"/> class.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="amazonSqs">The SQS client to send with.</param>
    /// <param name="topicAttributeKey">The message attribute the topic is written to (defaults to <see cref="SqsContextConverter{T}.DefaultTopicAttribute"/>).</param>
    public SqsBatchMessageClient(string queueUrl, IAmazonSQS amazonSqs, string topicAttributeKey = SqsContextConverter<object>.DefaultTopicAttribute)
    {
        _queueUrl = queueUrl;
        _amazonSqs = amazonSqs;
        _topicAttributeKey = topicAttributeKey;
        _serializer = new JsonSerializer();
    }

    /// <inheritdoc />
    public async Task<BatchSendResult> SendBatchAsync<TRequest>(IReadOnlyCollection<IBenzeneClientRequest<TRequest>> requests)
    {
        var converter = new SqsContextConverter<TRequest>(_queueUrl, _serializer, _topicAttributeKey);
        var failures = new List<FailedBatchEntry>();

        foreach (var chunk in BatchSend.Chunk(requests, MaxBatchSize))
        {
            var entries = new List<SendMessageBatchRequestEntry>(chunk.Count);
            foreach (var (request, index) in chunk)
            {
                try
                {
                    var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));
                    entries.Add(new SendMessageBatchRequestEntry
                    {
                        // The entry id carries the caller's index so a failure maps straight back.
                        Id = index.ToString(),
                        MessageBody = context.Request.MessageBody,
                        MessageAttributes = context.Request.MessageAttributes,
                    });
                }
                catch (Exception ex)
                {
                    // A single entry that can't be built (e.g. a serialization failure) fails just that
                    // entry, rather than aborting the whole batch after earlier chunks already sent.
                    failures.Add(new FailedBatchEntry(index, ex.GetType().Name, ex.Message));
                }
            }

            if (entries.Count == 0)
            {
                continue;
            }

            try
            {
                var response = await _amazonSqs.SendMessageBatchAsync(new SendMessageBatchRequest
                {
                    QueueUrl = _queueUrl,
                    Entries = entries,
                });

                // Guard the collection: AWS SDK v3.7 auto-initializes it to empty, but a v4 upgrade leaves
                // an unset collection null (would NRE on every all-success batch). Matches EventBridge.
                if (response.Failed != null)
                {
                    foreach (var failed in response.Failed)
                    {
                        failures.Add(new FailedBatchEntry(int.Parse(failed.Id), failed.Code, failed.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                // A chunk-level transport failure (throttle, network, expired credentials) fails this
                // chunk's entries rather than escaping and discarding the successes and failures already
                // recorded for earlier chunks. The caller then resends only what's reported failed,
                // instead of the whole collection (which would re-deliver everything that did succeed).
                foreach (var entry in entries)
                {
                    failures.Add(new FailedBatchEntry(int.Parse(entry.Id), ex.GetType().Name, ex.Message));
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
