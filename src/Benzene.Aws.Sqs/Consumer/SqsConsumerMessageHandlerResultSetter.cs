using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Records a message handler's outcome onto <see cref="SqsConsumerMessageContext.MessageResult"/>.
/// Read by <see cref="SqsConsumerApplication"/> to support <see cref="SqsConsumerAckMode.PerMessage"/> -
/// under the default <see cref="SqsConsumerAckMode.WholeBatch"/>, the whole batch is still deleted
/// together regardless of any individual message's recorded result.
/// </summary>
public class SqsConsumerMessageHandlerResultSetter : MessageHandlerResultSetterBase<SqsConsumerMessageContext>;
