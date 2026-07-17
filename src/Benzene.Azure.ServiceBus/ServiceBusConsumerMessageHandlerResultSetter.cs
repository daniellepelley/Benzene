using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Records a message handler's outcome onto <see cref="ServiceBusConsumerContext.MessageResult"/>.
/// Read by <see cref="BenzeneServiceBusWorker"/> to support
/// <see cref="ServiceBusConsumerAckMode.Explicit"/> - under the default
/// <see cref="ServiceBusConsumerAckMode.AutoComplete"/>, settlement is decided by whether the
/// handler threw, regardless of the recorded result.
/// </summary>
public class ServiceBusConsumerMessageHandlerResultSetter : MessageMessageHandlerResultSetterBase<ServiceBusConsumerContext>;
