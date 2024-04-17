using Amazon.SQS;
using Benzene.Abstractions.Logging;

namespace Benzene.Clients.Aws.Sqs;

public class SqsBenzeneMessageClientFactory : IBenzeneMessageClientFactory
{
    private readonly IBenzeneLogger _logger;
    private readonly string _queueUrl;
    private readonly IAmazonSQS _amazonSqsClient;

    public SqsBenzeneMessageClientFactory(string queueUrl, IAmazonSQS amazonSqsClient, IBenzeneLogger logger)
    {
        _amazonSqsClient = amazonSqsClient;
        _queueUrl = queueUrl;
        _logger = logger;
    }

    public virtual IBenzeneMessageClient Create()
    {
        return new SqsBenzeneMessageClient(_queueUrl, _amazonSqsClient, _logger);
    }
    
    public IBenzeneMessageClient Create(string service, string topic)
    {
        return Create();
    }

}
