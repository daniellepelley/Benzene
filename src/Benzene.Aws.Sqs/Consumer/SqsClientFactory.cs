using Amazon.SQS;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsClientFactory : ISqsClientFactory
{
    public IAmazonSQS Create(string serviceUrl)
    {
        return new AmazonSQSClient(new AmazonSQSConfig
        {
            ServiceURL = serviceUrl
        });
    }
}
