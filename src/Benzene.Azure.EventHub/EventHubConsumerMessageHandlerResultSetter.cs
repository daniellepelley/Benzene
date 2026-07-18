using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Records a message handler's outcome onto <see cref="EventHubConsumerContext.MessageResult"/>.
/// Event Hubs has no per-event settlement, so the recorded result doesn't affect checkpointing -
/// see <see cref="EventHubConsumerContext.MessageResult"/>.
/// </summary>
public class EventHubConsumerMessageHandlerResultSetter : MessageHandlerResultSetterBase<EventHubConsumerContext>;
