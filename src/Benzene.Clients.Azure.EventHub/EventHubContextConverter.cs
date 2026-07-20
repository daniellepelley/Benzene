using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Results;

namespace Benzene.Clients.Azure.EventHub;

/// <summary>
/// Converts between a generic Benzene client context and an <see cref="EventHubSendMessageContext"/>,
/// so that a Benzene client pipeline can send messages via Azure Event Hubs.
/// </summary>
/// <typeparam name="T">The type of the outgoing message.</typeparam>
public class EventHubContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, EventHubSendMessageContext>
{
    /// <summary>
    /// The default event-property key the topic is written to. It is a single default, not a
    /// hard-coded value — pass a different key to interoperate with a consumer that routes on another
    /// property. Keep it in sync with the consumer's property key.
    /// </summary>
    public const string DefaultTopicProperty = "topic";

    private readonly ISerializer _serializer;
    private readonly string _topicPropertyKey;
    private readonly string _partitionKeyHeader;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubContextConverter{T}"/> class using a
    /// <see cref="JsonSerializer"/> to serialize the outgoing message.
    /// </summary>
    /// <param name="topicPropertyKey">The event property the topic is written to (defaults to <see cref="DefaultTopicProperty"/>).</param>
    /// <param name="partitionKeyHeader">
    /// The request header whose value becomes the Event Hubs partition key (co-locating related events
    /// on one partition, preserving their order). <c>null</c> (the default) sends with no partition key.
    /// </param>
    public EventHubContextConverter(string topicPropertyKey = DefaultTopicProperty, string partitionKeyHeader = null)
        : this(new JsonSerializer(), topicPropertyKey, partitionKeyHeader)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubContextConverter{T}"/> class.
    /// </summary>
    /// <param name="serializer">The serializer used to serialize the outgoing message.</param>
    /// <param name="topicPropertyKey">The event property the topic is written to (defaults to <see cref="DefaultTopicProperty"/>).</param>
    /// <param name="partitionKeyHeader">The request header whose value becomes the partition key (defaults to <c>null</c> - no key).</param>
    public EventHubContextConverter(ISerializer serializer, string topicPropertyKey = DefaultTopicProperty, string partitionKeyHeader = null)
    {
        _serializer = serializer;
        _topicPropertyKey = topicPropertyKey;
        _partitionKeyHeader = partitionKeyHeader;
    }

    /// <summary>
    /// Builds an Event Hubs send context, serializing the outgoing message as the event body and setting
    /// the topic and headers as event properties (the same properties the Event Hubs ingress reads to
    /// route and rehydrate headers).
    /// </summary>
    /// <param name="contextIn">The incoming Benzene client context.</param>
    /// <returns>A task that resolves to the built <see cref="EventHubSendMessageContext"/>.</returns>
    public Task<EventHubSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Void> contextIn)
    {
        var eventData = new EventData(_serializer.Serialize(contextIn.Request.Message));
        foreach (var header in contextIn.Request.Headers)
        {
            eventData.Properties[header.Key] = header.Value;
        }

        eventData.Properties[_topicPropertyKey] = contextIn.Request.Topic;

        string partitionKey = null;
        if (_partitionKeyHeader != null)
        {
            contextIn.Request.Headers.TryGetValue(_partitionKeyHeader, out partitionKey);
        }

        return Task.FromResult(new EventHubSendMessageContext(eventData, partitionKey));
    }

    /// <summary>
    /// Marks the incoming Benzene client context as accepted. Event Hubs has no request/response
    /// semantics beyond a send acknowledgement.
    /// </summary>
    /// <param name="contextIn">The incoming Benzene client context to set the response on.</param>
    /// <param name="contextOut">The completed <see cref="EventHubSendMessageContext"/>.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(IBenzeneClientContext<T, Void> contextIn, EventHubSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Accepted<Void>();
        return Task.CompletedTask;
    }
}
