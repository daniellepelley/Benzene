using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Extracts the raw body string from an SQS message received by the polling consumer.
/// </summary>
public class SqsConsumerMessageBodyGetter : IMessageBodyGetter<SqsConsumerMessageContext>
{
    /// <summary>
    /// Gets the raw body from the SQS message.
    /// </summary>
    /// <param name="context">The SQS consumer message context to extract the body from.</param>
    /// <returns>The message body.</returns>
    public string GetBody(SqsConsumerMessageContext context)
    {
        return context.Message.Body;
    }
}
