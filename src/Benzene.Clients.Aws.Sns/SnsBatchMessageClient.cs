using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.Sns;

/// <summary>
/// Publishes messages to an SNS topic using <c>PublishBatch</c> (up to 10 messages per call). Reuses
/// <see cref="SnsContextConverter{T}"/> to build each entry (message + attributes, and FIFO
/// group/dedup ids when configured), then reports per-entry failures against the caller's indices.
/// </summary>
public class SnsBatchMessageClient : IBenzeneBatchMessageClient
{
    /// <summary>The maximum number of messages SNS accepts in one <c>PublishBatch</c> call.</summary>
    public const int MaxBatchSize = 10;

    private readonly IAmazonSimpleNotificationService _amazonSns;
    private readonly string _topicArn;
    private readonly string _topicAttributeKey;
    private readonly SnsPublishOptions? _publishOptions;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnsBatchMessageClient"/> class.
    /// </summary>
    /// <param name="topicArn">The ARN of the SNS topic to publish to.</param>
    /// <param name="amazonSns">The SNS client to publish with.</param>
    /// <param name="topicAttributeKey">The message attribute the Benzene topic is written to (defaults to <see cref="SnsContextConverter{T}.DefaultTopicAttribute"/>).</param>
    /// <param name="publishOptions">Optional FIFO/numeric-typing publish options.</param>
    public SnsBatchMessageClient(string topicArn, IAmazonSimpleNotificationService amazonSns,
        string topicAttributeKey = SnsContextConverter<object>.DefaultTopicAttribute, SnsPublishOptions? publishOptions = null)
    {
        _topicArn = topicArn;
        _amazonSns = amazonSns;
        _topicAttributeKey = topicAttributeKey;
        _publishOptions = publishOptions;
        _serializer = new JsonSerializer();
    }

    /// <inheritdoc />
    public async Task<BatchSendResult> SendBatchAsync<TRequest>(IReadOnlyCollection<IBenzeneClientRequest<TRequest>> requests)
    {
        var converter = new SnsContextConverter<TRequest>(_topicArn, _serializer, _topicAttributeKey, _publishOptions);
        var failures = new List<FailedBatchEntry>();

        foreach (var chunk in BatchSend.Chunk(requests, MaxBatchSize))
        {
            var entries = new List<PublishBatchRequestEntry>(chunk.Count);
            foreach (var (request, index) in chunk)
            {
                try
                {
                    var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));
                    var publish = context.Request;
                    entries.Add(new PublishBatchRequestEntry
                    {
                        Id = index.ToString(),
                        Message = publish.Message,
                        MessageAttributes = publish.MessageAttributes,
                        MessageGroupId = publish.MessageGroupId,
                        MessageDeduplicationId = publish.MessageDeduplicationId,
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
                var response = await _amazonSns.PublishBatchAsync(new PublishBatchRequest
                {
                    TopicArn = _topicArn,
                    PublishBatchRequestEntries = entries,
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
                // recorded for earlier chunks - so the caller resends only what's reported failed.
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
