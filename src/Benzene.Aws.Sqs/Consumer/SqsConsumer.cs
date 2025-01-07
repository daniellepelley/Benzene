using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumer : IBenzeneWorker
{
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private readonly SqsConsumerApplication _sqsConsumerApplication;
    private readonly SqsConsumerConfig _sqsConsumerConfig;
    private readonly ISqsClientFactory _sqsClientFactory;

    public SqsConsumer(IServiceResolverFactory serviceResolverFactory,
        SqsConsumerApplication sqsConsumerApplication, SqsConsumerConfig sqsConsumerConfig, ISqsClientFactory sqsClientFactory)
    {
        _sqsClientFactory = sqsClientFactory;
        _sqsConsumerConfig = sqsConsumerConfig;
        _sqsConsumerApplication = sqsConsumerApplication;
        _serviceResolverFactory = serviceResolverFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var client = _sqsClientFactory.Create(_sqsConsumerConfig.ServiceUrl);
        do
        {
            try
            {
                var result = await client.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _sqsConsumerConfig.QueueUrl,
                    MessageAttributeNames = new[] { "All" }.ToList(),
                    MaxNumberOfMessages = _sqsConsumerConfig.MaxNumberOfMessages,
                    WaitTimeSeconds = 1
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
            catch (TaskCanceledException)
            {
            }
        }
        while (!cancellationToken.IsCancellationRequested);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
