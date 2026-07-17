namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// Benzene's own model of a Queue Storage message - dependency-free, mirroring how
/// <c>Benzene.Aws.Lambda.Kinesis</c> models its Lambda event. The isolated-worker
/// <c>QueueTrigger</c> most commonly binds the message as a <c>string</c> (its text), which maps to
/// <see cref="MessageText"/> alone; bind the SDK's <c>QueueMessage</c> instead if you want to carry
/// the metadata properties across too.
/// </summary>
public class QueueStorageMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueueStorageMessage"/> class.
    /// </summary>
    /// <param name="messageText">The message text.</param>
    public QueueStorageMessage(string messageText)
    {
        MessageText = messageText;
    }

    /// <summary>The message text (the queue message's body).</summary>
    public string MessageText { get; }

    /// <summary>The message id, when bound from the SDK's <c>QueueMessage</c>; otherwise null.</summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// How many times the message has been dequeued, when bound from the SDK's <c>QueueMessage</c>;
    /// otherwise null. Useful for poison-message handling before the host's own
    /// <c>maxDequeueCount</c> moves it to the <c>&lt;queue&gt;-poison</c> queue.
    /// </summary>
    public long? DequeueCount { get; init; }

    /// <summary>When the message was enqueued, when bound from the SDK's <c>QueueMessage</c>; otherwise null.</summary>
    public DateTimeOffset? InsertedOn { get; init; }
}
