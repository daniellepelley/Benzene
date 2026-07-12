using Amazon.SQS;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Creates <see cref="IAmazonSQS"/> clients by returning the injected client instance.
/// </summary>
public class SqsClientFactory : ISqsClientFactory
{
    private readonly IAmazonSQS _amazonSqs;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsClientFactory"/> class.
    /// </summary>
    /// <param name="amazonSqs">The SQS client to return from <see cref="Create"/>.</param>
    public SqsClientFactory(IAmazonSQS amazonSqs)
    {
        _amazonSqs = amazonSqs;
    }

    /// <summary>
    /// Returns the injected <see cref="IAmazonSQS"/> client.
    /// </summary>
    /// <returns>The SQS client.</returns>
    public IAmazonSQS Create()
    {
        return _amazonSqs;
    }
}
