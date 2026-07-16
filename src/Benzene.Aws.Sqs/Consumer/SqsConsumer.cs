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
/// (<see cref="SqsConsumerAckMode.WholeBatch"/>), messages are only deleted after the whole batch has
/// been processed; if any message's handler throws, none of the batch's messages are deleted - they're
/// all left on the queue to be retried (subject to the queue's visibility timeout and redrive policy).
/// Pass <see cref="SqsConsumerOptions"/> with <see cref="SqsConsumerAckMode.PerMessage"/> to instead
/// delete only the messages that actually succeeded, regardless of any others in the same batch
/// failing. A poll iteration that throws for a reason other than cancellation (e.g. a transient AWS
/// error) is logged and the loop continues, rather than the exception propagating out and permanently
/// ending the worker.
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
    /// instance (<see cref="SqsConsumerAckMode.WholeBatch"/>) if omitted.
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
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var client = _sqsClientFactory.Create();
        do
        {
            try
            {
                var result = await client.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _sqsConsumerConfig.QueueUrl,
                    MessageAttributeNames = new[] { "All" }.ToList(),
                    MaxNumberOfMessages = _sqsConsumerConfig.MaxNumberOfMessages,
                    WaitTimeSeconds = _sqsConsumerConfig.WaitTimeSeconds
                }, cancellationToken);

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
