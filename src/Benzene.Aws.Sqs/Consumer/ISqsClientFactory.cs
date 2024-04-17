using Amazon.SQS;

namespace Benzene.Aws.Sqs.Consumer;

public interface ISqsClientFactory
{
    IAmazonSQS Create(string serviceUrl);
}
