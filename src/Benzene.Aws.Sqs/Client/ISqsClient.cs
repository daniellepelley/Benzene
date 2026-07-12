using System.Threading.Tasks;

namespace Benzene.Aws.Sqs.Client;

/// <summary>
/// Represents a client for publishing messages to an SQS queue.
/// </summary>
public interface ISqsClient
{
    /// <summary>
    /// Publishes a message to the queue.
    /// </summary>
    /// <param name="topic">The topic to tag the message with.</param>
    /// <param name="message">The message body.</param>
    /// <param name="status">An optional status to tag the message with.</param>
    /// <returns>A task that resolves to the result status of the publish operation.</returns>
    Task<string> PublishAsync(string topic, string message, string status);
}
