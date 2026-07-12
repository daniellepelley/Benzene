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
/// <see cref="StartAsync"/>'s cancellation token is signaled. Messages are only deleted after the whole
/// batch has been processed; if processing throws, the messages are left on the queue to be retried
/// (subject to the queue's visibility timeout and redrive policy). A poll iteration that throws for a
/// reason other than cancellation (e.g. a transient AWS error) is logged and the loop continues, rather
/// than the exception propagating out and permanently ending the worker.
/// </remarks>
public class SqsConsumer : IBenzeneWorker
{
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private readonly SqsConsumerApplication _sqsConsumerApplication;
    private readonly SqsConsumerConfig _sqsConsumerConfig;
    private readonly ISqsClientFactory _sqsClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsConsumer"/> class.
    /// </summary>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each batch.</param>
    /// <param name="sqsConsumerApplication">The application that runs each batch of messages through the middleware pipeline.</param>
    /// <param name="sqsConsumerConfig">The queue URL and batch size to poll with.</param>
    /// <param name="sqsClientFactory">The factory used to create the underlying SQS client.</param>
    public SqsConsumer(IServiceResolverFactory serviceResolverFactory,
        SqsConsumerApplication sqsConsumerApplication, SqsConsumerConfig sqsConsumerConfig, ISqsClientFactory sqsClientFactory)
    {
        _sqsClientFactory = sqsClientFactory;
        _sqsConsumerConfig = sqsConsumerConfig;
        _sqsConsumerApplication = sqsConsumerApplication;
        _serviceResolverFactory = serviceResolverFactory;
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
                    await _sqsConsumerApplication.HandleAsync(result, _serviceResolverFactory);

                    await client.DeleteMessageBatchAsync(new DeleteMessageBatchRequest
                    {
                        QueueUrl = _sqsConsumerConfig.QueueUrl,
                        Entries = result.Messages
                            .Select(x => new DeleteMessageBatchRequestEntry(x.MessageId, x.ReceiptHandle))
                            .ToList()
                    }, cancellationToken);
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
