using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Records the message handler result onto an <see cref="SqsMessageContext"/>'s
/// <see cref="SqsMessageContext.MessageResult"/>, so <see cref="SqsApplication"/> can report failed
/// records back to SQS for retry.
/// </summary>
public class SqsMessageHandlerResultSetter : MessageHandlerResultSetterBase<SqsMessageContext>;
