using Benzene.Core.MessageHandlers;

namespace Benzene.RabbitMq.RabbitMqMessage;

/// <summary>
/// Records a message handler's outcome onto <see cref="RabbitMqContext.MessageResult"/>. Read by
/// <see cref="RabbitMqWorker"/> to decide ack vs nack under <see cref="RabbitMqAckMode.Explicit"/>.
/// </summary>
public class RabbitMqMessageHandlerResultSetter : MessageHandlerResultSetterBase<RabbitMqContext>;
