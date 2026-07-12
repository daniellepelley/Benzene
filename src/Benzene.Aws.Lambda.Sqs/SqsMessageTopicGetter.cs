using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Extracts the message topic from an SQS message's <c>topic</c> message attribute.
/// </summary>
public class SqsMessageTopicGetter : IMessageTopicGetter<SqsMessageContext>
{
    /// <summary>
    /// Gets the topic from the SQS message's <c>topic</c> attribute.
    /// </summary>
    /// <param name="context">The SQS message context to extract the topic from.</param>
    /// <returns>The topic, or a topic with a null ID if the <c>topic</c> attribute isn't present.</returns>
    public ITopic GetTopic(SqsMessageContext context)
    {
        return new Topic(GetFromAttributes(context, "topic"));
    }

    private static string GetFromAttributes(SqsMessageContext context, string key)
    {
        if (!context.SqsMessage.MessageAttributes.ContainsKey(key))
        {
            return null;
        }

        return context.SqsMessage.MessageAttributes[key].StringValue;
    }
}
