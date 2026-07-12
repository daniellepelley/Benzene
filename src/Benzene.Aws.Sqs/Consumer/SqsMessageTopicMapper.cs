using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Extracts the message topic from an SQS message's <c>topic</c> message attribute.
/// </summary>
public class SqsConsumerMessageTopicGetter : IMessageTopicGetter<SqsConsumerMessageContext>
{
    /// <summary>
    /// Gets the topic from the SQS message's <c>topic</c> attribute.
    /// </summary>
    /// <param name="context">The SQS consumer message context to extract the topic from.</param>
    /// <returns>The topic, or a topic with a null ID if the <c>topic</c> attribute isn't present.</returns>
    public ITopic GetTopic(SqsConsumerMessageContext context)
    {
        return new Topic(GetFromAttributes(context, "topic"));
    }

    private static string GetFromAttributes(SqsConsumerMessageContext context, string key)
    {
        if (!context.Message.MessageAttributes.ContainsKey(key))
        {
            return null;
        }

        return context.Message.MessageAttributes[key].StringValue;
    }
}
