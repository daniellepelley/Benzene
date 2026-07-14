using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// Resolves the message topic from the event's <c>detail-type</c> (plan decision E1) — EventBridge's
/// native routing key, so no <c>topic</c> attribute needs bolting on the way SQS/SNS require.
/// </summary>
public class EventBridgeMessageTopicGetter : IMessageTopicGetter<EventBridgeContext>
{
    public ITopic GetTopic(EventBridgeContext context)
    {
        return new Topic(context.Event.DetailType);
    }
}
