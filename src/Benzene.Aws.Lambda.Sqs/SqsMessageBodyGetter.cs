using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Extracts the raw body string from an SQS message.
/// </summary>
public class SqsMessageBodyGetter : IMessageBodyGetter<SqsMessageContext>
{
    /// <summary>
    /// Gets the raw body from the SQS message.
    /// </summary>
    /// <param name="context">The SQS message context to extract the body from.</param>
    /// <returns>The message body.</returns>
    public string GetBody(SqsMessageContext context)
    {
        return context.SqsMessage.Body;
    }
}
