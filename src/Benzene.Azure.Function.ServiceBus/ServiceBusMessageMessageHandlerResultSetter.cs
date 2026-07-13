using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Sets the message handler result on a <see cref="ServiceBusContext"/>. A no-op setter: the Service Bus
/// trigger completes the message automatically per the trigger's default settings, regardless of the
/// handler's result.
/// </summary>
public class ServiceBusMessageMessageHandlerResultSetter : DefaultMessageMessageHandlerResultSetterBase<ServiceBusContext>;
