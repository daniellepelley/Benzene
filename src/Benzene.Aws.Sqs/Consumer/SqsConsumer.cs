using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Microsoft.Extensions.Logging;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// A long-running worker that continuously polls an SQS queue, runs received messages through the
/// middleware pipeline as a batch, and deletes them once handled.
/// </summary>
/// <remarks>
/// Uses long polling (<see cref="SqsConsumerConfig.WaitTimeSeconds"/>) in a loop until
/// <see cref="StartAsync"/>'s cancellation token is signaled. By default
/// (<see cref="SqsConsumerAckMode.PerMessage"/>), only the messages whose handler reported an explicit
/// success are deleted; a message that fails, throws, or is unrouted is left on the queue individually
/// for redelivery/DLQ redrive, regardless of the other messages in the same batch. Pass
/// <see cref="SqsConsumerOptions"/> with <see cref="SqsConsumerAckMode.WholeBatch"/> for the older
/// all-or-nothing-on-throw behavior (the whole batch is deleted together, only once every message has
/// run without throwing). A poll iteration that throws for a reason other than cancellation (e.g. a
/// transient AWS error) is logged and the loop continues after a capped, geometrically-growing backoff
/// (reset on the next successful receive), rather than the exception propagating out and permanently
/// ending the worker - or a persistent failure spinning the loop with no delay.
/// </remarks>
public class SqsConsumer : IBenzeneWorker
{
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private readonly SqsConsumerApplication _sqsConsumerApplication;
    private readonly SqsConsumerConfig _sqsConsumerConfig;
    private readonly ISqsClientFactory _sqsClientFactory;
    private readonly SqsConsumerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsConsumer"/> class.
    /// </summary>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each batch.</param>
    /// <param name="sqsConsumerApplication">The application that runs each batch of messages through the middleware pipeline.</param>
    /// <param name="sqsConsumerConfig">The queue URL and batch size to poll with.</param>
    /// <param name="sqsClientFactory">The factory used to create the underlying SQS client.</param>
    /// <param name="options">
    /// Configures how the batch is acknowledged. Defaults to a new <see cref="SqsConsumerOptions"/>
    /// instance (<see cref="SqsConsumerAckMode.PerMessage"/>) if omitted.
    /// </param>
    public SqsConsumer(IServiceResolverFactory serviceResolverFactory,
        SqsConsumerApplication sqsConsumerApplication, SqsConsumerConfig sqsConsumerConfig, ISqsClientFactory sqsClientFactory,
        SqsConsumerOptions options = null)
    {
        _sqsClientFactory = sqsClientFactory;
        _sqsConsumerConfig = sqsConsumerConfig;
        _sqsConsumerApplication = sqsConsumerApplication;
        _serviceResolverFactory = serviceResolverFactory;
        _options = options ?? new SqsConsumerOptions();
    }

    /// <summary>
    /// Starts the poll loop, running until <paramref name="cancellationToken"/> is signaled.
    /// </summary>
    /// <param name="cancellationToken">The token used to stop the poll loop.</param>
    /// <returns>A task that completes when the poll loop stops.</returns>
    /// <summary>The ceiling the error-path backoff grows toward on repeated receive failures.</summary>
    private static readonly TimeSpan MaxErrorBackoff = TimeSpan.FromSeconds(30);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var client = _sqsClientFactory.Create();
        var consecutiveFailures = 0;
        do
        {
            try
            {
                var result = await client.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _sqsConsumerConfig.QueueUrl,
                    MessageAttributeNames = new[] { "All" }.ToList(),
                    // Request the ApproximateReceiveCount system attribute so a handler can make
                    // poison-message decisions off SqsConsumerMessageContext.ApproximateReceiveCount.
                    MessageSystemAttributeNames = new[] { "ApproximateReceiveCount" }.ToList(),
                    MaxNumberOfMessages = _sqsConsumerConfig.MaxNumberOfMessages,
                    WaitTimeSeconds = _sqsConsumerConfig.WaitTimeSeconds
                }, cancellationToken);

                // A receive (poll) that returns - even empty - means the queue is reachable again, so
                // clear the consecutive-failure count that drives the error backoff.
                consecutiveFailures = 0;

                if (result.Messages.Any())
                {
                    var batchResult = await _sqsConsumerApplication.HandleAsync(result, _serviceResolverFactory);

                    // Under WholeBatch (the default), a thrown exception above already skipped this
                    // whole block via the outer catch, so reaching here means every message
                    // succeeded (or failed only via a non-throwing result, which WholeBatch still
                    // deletes along with the rest, unchanged from prior behavior) - delete everything.
                    // Under PerMessage, delete only what actually succeeded.
                    var messagesToDelete = _options.AckMode == SqsConsumerAckMode.PerMessage
                        ? batchResult.SuccessfulMessages
                        : result.Messages;

                    if (messagesToDelete.Count > 0)
                    {
                        await client.DeleteMessageBatchAsync(new DeleteMessageBatchRequest
                        {
                            QueueUrl = _sqsConsumerConfig.QueueUrl,
                            Entries = messagesToDelete
                                .Select(x => new DeleteMessageBatchRequestEntry(x.MessageId, x.ReceiptHandle))
                                .ToList()
                        }, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellationToken is signaled mid-poll; the loop condition below exits cleanly.
            }
            catch (Exception ex)
            {
                using var loggingScope = _serviceResolverFactory.CreateScope();
                loggingScope.GetService<ILogger<SqsConsumer>>()
                    .LogError(ex, "SQS poll iteration for queue {queueUrl} failed", _sqsConsumerConfig.QueueUrl);

                // The first failure retries immediately, so a lone transient blip recovers with no
                // added latency. Only a *persistent* failure (bad queue URL, denied/expired
                // credentials, throttling, an AZ outage) backs off - otherwise re-issuing
                // ReceiveMessageAsync immediately would be a tight loop that spins the CPU, floods the
                // logs, and amplifies API throttling. Delay grows geometrically from the second
                // consecutive failure up to a cap, and resets on the next successful receive.
                consecutiveFailures++;
                if (consecutiveFailures > 1)
                {
                    var delaySeconds = Math.Min(
                        Math.Pow(2, consecutiveFailures - 2), MaxErrorBackoff.TotalSeconds);
                    try
                    {
                        // Cancellation-aware so shutdown still drains promptly rather than waiting out the delay.
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Shutdown requested during the backoff; the loop condition below exits cleanly.
                    }
                }
            }
        }
        while (!cancellationToken.IsCancellationRequested);
    }

    /// <summary>
    /// Stops the worker. No cleanup is required beyond exiting the poll loop in <see cref="StartAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the stop operation.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
