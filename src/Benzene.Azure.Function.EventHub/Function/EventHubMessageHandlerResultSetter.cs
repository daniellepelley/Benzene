using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// Records a message handler's outcome onto <see cref="EventHubContext.MessageResult"/> on the
/// property-based routing path (<c>UseMessageHandlers()</c> directly on <see cref="EventHubContext"/>).
/// The Event Hubs trigger has no per-event settlement — the host checkpoints the whole batch — so this
/// is read for diagnostics and <see cref="EventHubOptions.RaiseOnFailureStatus"/>. Mirrors
/// <c>Benzene.Azure.Function.ServiceBus</c>'s <c>ServiceBusMessageHandlerResultSetter</c>.
/// </summary>
public class EventHubMessageHandlerResultSetter : MessageHandlerResultSetterBase<EventHubContext>;
