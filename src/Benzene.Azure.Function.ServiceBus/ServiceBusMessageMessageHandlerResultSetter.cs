using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Records a message handler's outcome onto <see cref="ServiceBusContext.MessageResult"/>. The
/// Service Bus trigger still completes the message automatically per the trigger's default settings
/// regardless of the handler's result (no <c>ServiceBusMessageActions</c> wiring - see the package's
/// <c>CLAUDE.md</c>), but <see cref="ServiceBusBatchApplication"/> reads this to support
/// <see cref="ServiceBusOptions.RaiseOnFailureStatus"/>.
/// </summary>
public class ServiceBusMessageMessageHandlerResultSetter : MessageMessageHandlerResultSetterBase<ServiceBusContext>;
