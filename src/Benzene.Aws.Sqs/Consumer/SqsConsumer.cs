using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Benzene.Abstractions.DI;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumer
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

    public async Task Start(CancellationToken token)
    {
        using var client = _sqsClientFactory.Create(_sqsConsumerConfig.ServiceUrl);
        do
        {
            var result = await client.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _sqsConsumerConfig.QueueUrl,
                MessageAttributeNames = new[] { "All" }.ToList(),
                MaxNumberOfMessages = _sqsConsumerConfig.MaxNumberOfMessages,
                WaitTimeSeconds = 10
            }, token);

            await _sqsConsumerApplication.HandleAsync(result, _serviceResolverFactory.CreateScope());

            await client.DeleteMessageBatchAsync(new DeleteMessageBatchRequest
            {
                QueueUrl = _sqsConsumerConfig.QueueUrl,
                Entries = result.Messages
                    .Select(x => new DeleteMessageBatchRequestEntry(x.MessageId, x.ReceiptHandle))
                    .ToList()
            }, token);
        } while (!token.IsCancellationRequested);
    }
}
