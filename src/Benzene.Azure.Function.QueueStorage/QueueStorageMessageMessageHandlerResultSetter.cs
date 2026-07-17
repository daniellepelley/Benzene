using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// Records a message handler's outcome onto <see cref="QueueStorageContext.MessageResult"/>. The
/// Queue Storage trigger has no per-message settlement (success deletes the message, an exception
/// retries it and eventually moves it to the poison queue), so the result is recorded for
/// middleware/diagnostics rather than written back to the transport.
/// </summary>
public class QueueStorageMessageMessageHandlerResultSetter : MessageMessageHandlerResultSetterBase<QueueStorageContext>;
