using Amazon.SQS;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsClientFactory : ISqsClientFactory
{
    private readonly IAmazonSQS _amazonSqs;

    public SqsClientFactory(IAmazonSQS amazonSqs)
    {
        _amazonSqs = amazonSqs;
    }
    
    public IAmazonSQS Create()
    {
        return _amazonSqs;
    }
}
