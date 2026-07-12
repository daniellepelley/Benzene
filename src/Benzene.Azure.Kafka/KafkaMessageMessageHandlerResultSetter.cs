using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.Kafka;

/// <summary>
/// Sets the message handler result on a <see cref="KafkaContext"/>. A no-op setter, since the Kafka
/// trigger doesn't track per-record results the way batch-failure-reporting transports do.
/// </summary>
public class KafkaMessageMessageHandlerResultSetter : DefaultMessageMessageHandlerResultSetterBase<KafkaContext>;
