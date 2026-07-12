using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// A no-op message handler result setter for the SQS polling consumer — messages are deleted from the
/// queue after the batch is processed regardless of individual handler outcome, so no per-message result
/// tracking is needed.
/// </summary>
public class SqsConsumerMessageMessageHandlerResultSetter : DefaultMessageMessageHandlerResultSetterBase<SqsConsumerMessageContext>;
