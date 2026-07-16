namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Controls whether <see cref="SqsConsumer"/> deletes an entire poll batch as a unit, or deletes
/// only the messages that succeeded.
/// </summary>
public enum SqsConsumerAckMode
{
    /// <summary>
    /// The original default. Every message in the poll batch is deleted together, only once the
    /// whole batch has finished processing without any message throwing. If any message's handler
    /// throws, no messages in the batch are deleted - the entire batch is left on the queue to be
    /// retried (subject to the queue's visibility timeout and redrive policy). A message that fails
    /// without throwing (an unsuccessful, non-exception result) does not by itself prevent deletion
    /// in this mode - only a thrown exception does.
    /// </summary>
    WholeBatch = 0,

    /// <summary>
    /// Only the messages that succeeded (no thrown exception, and no unsuccessful result) are
    /// deleted; messages that failed are left on the queue individually, so only they are retried -
    /// the rest of the batch isn't reprocessed. A message's handler throwing no longer aborts the
    /// whole poll iteration in this mode.
    /// </summary>
    PerMessage = 1,
}
