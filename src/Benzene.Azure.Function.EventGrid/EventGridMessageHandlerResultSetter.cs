using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.Function.EventGrid;

/// <summary>
/// Records a message handler's outcome onto <see cref="EventGridContext.MessageResult"/>. Event Grid
/// has no per-event settlement from inside the handler (a thrown exception is what drives its
/// retry/dead-letter machinery), so the result is recorded for middleware/diagnostics rather than
/// written back to the transport.
/// </summary>
public class EventGridMessageHandlerResultSetter : MessageHandlerResultSetterBase<EventGridContext>;
