using Benzene.Abstractions.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Kafka;

public class KafkaMessageResultSetter : MessageResultSetterBase<KafkaContext>;