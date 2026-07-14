using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Aws.S3;

/// <summary>
/// Extracts the message topic from an S3 event notification record's event name (e.g.
/// <c>ObjectCreated:Put</c>), so records route to a message handler declaring that topic.
/// </summary>
public class S3MessageTopicGetter : IMessageTopicGetter<S3RecordContext>
{
    /// <summary>
    /// Gets the topic from the S3 record's <see cref="Amazon.Lambda.S3Events.S3Event.S3EventNotificationRecord.EventName"/>.
    /// </summary>
    /// <param name="context">The S3 record context to extract the topic from.</param>
    /// <returns>A topic whose id is the S3 event name.</returns>
    public ITopic GetTopic(S3RecordContext context)
    {
        return new Topic(context.S3EventNotificationRecord.EventName);
    }
}
