using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Results;

namespace Benzene.Clients.Azure.ServiceBus;

/// <summary>
/// Converts between a generic Benzene client context and a <see cref="ServiceBusSendMessageContext"/>,
/// so that a Benzene client pipeline can send messages via Azure Service Bus.
/// </summary>
/// <typeparam name="T">The type of the outgoing message.</typeparam>
public class ServiceBusContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, ServiceBusSendMessageContext>
{
    /// <summary>
    /// The default application-property key the topic is written to. It is a single default, not a
    /// hard-coded value — pass a different key to interoperate with a consumer that routes on another
    /// application property. Keep it in sync with the consumer's property key.
    /// </summary>
    public const string DefaultTopicProperty = "topic";

    private readonly ISerializer _serializer;
    private readonly string _topicPropertyKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusContextConverter{T}"/> class using a
    /// <see cref="JsonSerializer"/> to serialize the outgoing message.
    /// </summary>
    /// <param name="topicPropertyKey">The application property the topic is written to (defaults to <see cref="DefaultTopicProperty"/>).</param>
    public ServiceBusContextConverter(string topicPropertyKey = DefaultTopicProperty)
        : this(new JsonSerializer(), topicPropertyKey)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusContextConverter{T}"/> class.
    /// </summary>
    /// <param name="serializer">The serializer used to serialize the outgoing message.</param>
    /// <param name="topicPropertyKey">The application property the topic is written to (defaults to <see cref="DefaultTopicProperty"/>).</param>
    public ServiceBusContextConverter(ISerializer serializer, string topicPropertyKey = DefaultTopicProperty)
    {
        _serializer = serializer;
        _topicPropertyKey = topicPropertyKey;
    }

    /// <summary>
    /// Builds a Service Bus send context, serializing the outgoing message as the message body and
    /// setting the topic and headers as application properties (the same properties the Service Bus
    /// ingress reads to route and rehydrate headers).
    /// </summary>
    /// <param name="contextIn">The incoming Benzene client context.</param>
    /// <returns>A task that resolves to the built <see cref="ServiceBusSendMessageContext"/>.</returns>
    public Task<ServiceBusSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Void> contextIn)
    {
        var message = new ServiceBusMessage(_serializer.Serialize(contextIn.Request.Message));
        foreach (var header in contextIn.Request.Headers)
        {
            message.ApplicationProperties[header.Key] = header.Value;
        }

        message.ApplicationProperties[_topicPropertyKey] = contextIn.Request.Topic;

        return Task.FromResult(new ServiceBusSendMessageContext(message));
    }

    /// <summary>
    /// Marks the incoming Benzene client context as accepted. Service Bus has no request/response
    /// semantics beyond a send acknowledgement.
    /// </summary>
    /// <param name="contextIn">The incoming Benzene client context to set the response on.</param>
    /// <param name="contextOut">The completed <see cref="ServiceBusSendMessageContext"/>.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(IBenzeneClientContext<T, Void> contextIn, ServiceBusSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Accepted<Void>();
        return Task.CompletedTask;
    }
}
