using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// Provides the headers for a Queue Storage message - always empty, because Queue Storage messages
/// have no properties/attributes (the body is the entire message). Headers carried inside a Benzene
/// message envelope body are surfaced by the <c>UseBenzeneMessage(...)</c> path instead.
/// </summary>
public class QueueStorageMessageHeadersGetter : IMessageHeadersGetter<QueueStorageContext>
{
    /// <summary>
    /// Gets an empty header dictionary - Queue Storage messages carry no transport-level headers.
    /// </summary>
    /// <param name="context">The Queue Storage context.</param>
    /// <returns>An empty dictionary.</returns>
    public IDictionary<string, string> GetHeaders(QueueStorageContext context)
    {
        return new Dictionary<string, string>();
    }
}
