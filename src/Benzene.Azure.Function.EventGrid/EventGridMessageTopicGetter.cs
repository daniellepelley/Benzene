using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Azure.Function.EventGrid;

/// <summary>
/// Extracts the message topic from an Event Grid event's <see cref="EventGridTriggerEvent.EventType"/>
/// (e.g. <c>Microsoft.Storage.BlobCreated</c>, or your own custom event type), so events route to a
/// message handler declaring that topic - the same shape as <c>Benzene.Aws.Lambda.S3</c> routing on
/// the S3 event name.
/// </summary>
public class EventGridMessageTopicGetter : IMessageTopicGetter<EventGridContext>
{
    /// <summary>
    /// Gets the topic from the event's <see cref="EventGridTriggerEvent.EventType"/>.
    /// </summary>
    /// <param name="context">The Event Grid context to extract the topic from.</param>
    /// <returns>A topic whose id is the event type, or null if the event carries none.</returns>
    public ITopic? GetTopic(EventGridContext context)
    {
        return context.Event.EventType == null ? null : new Topic(context.Event.EventType);
    }
}
