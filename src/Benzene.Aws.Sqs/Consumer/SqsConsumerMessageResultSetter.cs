using Benzene.Abstractions.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumerMessageResultSetter : MessageResultSetterBase<SqsConsumerMessageContext>;