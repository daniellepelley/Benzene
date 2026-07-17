using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;

namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// The transport's own topic getter for Queue Storage - which always returns <c>null</c>, because a
/// Queue Storage message carries no properties/attributes a topic could ride on (unlike Service Bus
/// application properties or Kafka topics); the body is the entire message. Routing therefore comes
/// from either a preset topic (<c>UsePresetTopic(...)</c>, via the
/// <c>PresetTopicMessageTopicGetter</c> this getter is wrapped in) or a Benzene message envelope in
/// the body (<c>UseBenzeneMessage(...)</c>).
/// </summary>
public class QueueStorageMessageTopicGetter : IMessageTopicGetter<QueueStorageContext>
{
    /// <summary>
    /// Always returns <c>null</c> - see the class summary for where Queue Storage routing actually
    /// comes from.
    /// </summary>
    /// <param name="context">The Queue Storage context.</param>
    /// <returns><c>null</c>.</returns>
    public ITopic? GetTopic(QueueStorageContext context) => null;
}
