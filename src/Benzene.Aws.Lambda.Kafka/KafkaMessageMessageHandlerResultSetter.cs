using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Records the message handler result onto a <see cref="KafkaContext"/>'s
/// <see cref="KafkaContext.MessageResult"/>.
/// </summary>
public class KafkaMessageMessageHandlerResultSetter : MessageMessageHandlerResultSetterBase<KafkaContext>;
