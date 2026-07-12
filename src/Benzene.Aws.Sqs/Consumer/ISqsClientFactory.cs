using Amazon.SQS;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Creates the underlying <see cref="IAmazonSQS"/> client used by <see cref="SqsConsumer"/> to poll a queue.
/// </summary>
public interface ISqsClientFactory
{
    /// <summary>
    /// Creates an <see cref="IAmazonSQS"/> client.
    /// </summary>
    /// <returns>The created client.</returns>
    IAmazonSQS Create();
}
