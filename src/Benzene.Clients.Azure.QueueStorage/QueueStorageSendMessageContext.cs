namespace Benzene.Clients.Azure.QueueStorage;

/// <summary>
/// Provides the middleware pipeline context for sending a single message to Azure Queue Storage. A
/// queue message is a plain string body - there is no properties/attributes bag on this transport.
/// </summary>
public class QueueStorageSendMessageContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueueStorageSendMessageContext"/> class.
    /// </summary>
    /// <param name="messageText">The message text to send.</param>
    public QueueStorageSendMessageContext(string messageText)
    {
        MessageText = messageText;
    }

    /// <summary>
    /// Gets the message text to send.
    /// </summary>
    public string MessageText { get; }

    /// <summary>
    /// Gets or sets whether the message was sent. Set by <see cref="QueueStorageClientMiddleware"/>
    /// once the send completes without throwing. <see cref="Azure.Storage.Queues.QueueClient.SendMessageAsync(string, System.Threading.CancellationToken)"/>
    /// returns a receipt with no meaningful payload for Benzene's purposes, so a completed send is an
    /// acknowledgement only.
    /// </summary>
    public bool IsSent { get; set; }
}
