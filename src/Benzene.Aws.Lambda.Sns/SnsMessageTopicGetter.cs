using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Extracts the message topic from an SNS record's <c>topic</c> message attribute.
/// </summary>
public class SnsMessageTopicGetter : IMessageTopicGetter<SnsRecordContext>
{
    /// <summary>
    /// Gets the topic from the SNS record's <c>topic</c> attribute.
    /// </summary>
    /// <param name="context">The SNS record context to extract the topic from.</param>
    /// <returns>The topic, or a topic with a null ID if the <c>topic</c> attribute isn't present.</returns>
    public ITopic GetTopic(SnsRecordContext context)
    {
        return new Topic(SnsUtils.GetFromAttributes(context, "topic"));
    }
}
