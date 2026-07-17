using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// Provides the middleware pipeline context for a single Queue Storage message within an Azure
/// Functions Queue Storage trigger invocation.
/// </summary>
public class QueueStorageContext : IHasMessageResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueueStorageContext"/> class.
    /// </summary>
    /// <param name="message">The Queue Storage message.</param>
    public QueueStorageContext(QueueStorageMessage message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the Queue Storage message.
    /// </summary>
    public QueueStorageMessage Message { get; }

    /// <summary>
    /// Gets or sets the result of handling this message. The Queue Storage trigger has no
    /// per-message settlement (the host deletes the message when the invocation succeeds and
    /// retries/poisons it when it throws), so this is recorded for middleware/diagnostics only.
    /// </summary>
    public IMessageResult MessageResult { get; set; } = null!;
}
