using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// Extracts the message body from a Queue Storage message - its message text.
/// </summary>
public class QueueStorageMessageBodyGetter : IMessageBodyGetter<QueueStorageContext>
{
    /// <summary>
    /// Gets the Queue Storage message's text.
    /// </summary>
    /// <param name="context">The Queue Storage context to extract the body from.</param>
    /// <returns>The message text.</returns>
    public string GetBody(QueueStorageContext context)
    {
        return context.Message.MessageText;
    }
}
