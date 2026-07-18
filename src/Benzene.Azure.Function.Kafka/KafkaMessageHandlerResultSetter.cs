using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.Function.Kafka;

/// <summary>
/// Records a message handler's outcome onto <see cref="KafkaContext.MessageResult"/>. The Kafka
/// trigger has no platform-level per-record acknowledgement to report back to, but
/// <see cref="KafkaBatchApplication"/> reads this to support <see cref="KafkaOptions.RaiseOnFailureStatus"/>.
/// </summary>
public class KafkaMessageHandlerResultSetter : MessageHandlerResultSetterBase<KafkaContext>;
