using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.ServiceBus;

/// <summary>
/// Converts between an outbound <see cref="OutboundContext"/> and a
/// <see cref="ServiceBusSendMessageContext"/>, so an outbound route
/// (<c>OutboundRoutingBuilder.Route</c>) can send via Azure Service Bus. The
/// <see cref="OutboundContext"/> counterpart of <see cref="ServiceBusContextConverter{T}"/>.
/// </summary>
/// <remarks>
/// Service Bus has no request/response semantics beyond a send acknowledgement, so the response this
/// converter produces is always <see cref="IBenzeneResult{Void}"/> - a topic routed here must be sent
/// via <c>IBenzeneMessageSender.SendAsync&lt;TRequest,Void&gt;</c>.
/// </remarks>
public class OutboundServiceBusContextConverter : IContextConverter<OutboundContext, ServiceBusSendMessageContext>
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
    /// Initializes a new instance of the <see cref="OutboundServiceBusContextConverter"/> class using a
    /// <see cref="JsonSerializer"/> to serialize the outgoing message.
    /// </summary>
    /// <param name="topicPropertyKey">The application property the topic is written to (defaults to <see cref="DefaultTopicProperty"/>).</param>
    public OutboundServiceBusContextConverter(string topicPropertyKey = DefaultTopicProperty)
        : this(new JsonSerializer(), topicPropertyKey)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundServiceBusContextConverter"/> class.
    /// </summary>
    /// <param name="serializer">The serializer used to serialize the outgoing message.</param>
    /// <param name="topicPropertyKey">The application property the topic is written to (defaults to <see cref="DefaultTopicProperty"/>).</param>
    public OutboundServiceBusContextConverter(ISerializer serializer, string topicPropertyKey = DefaultTopicProperty)
    {
        _serializer = serializer;
        _topicPropertyKey = topicPropertyKey;
    }

    /// <summary>
    /// Builds a Service Bus message, serializing the outgoing message as the body and setting the topic
    /// and headers as application properties.
    /// </summary>
    /// <param name="contextIn">The outbound context to convert.</param>
    /// <returns>A task that resolves to the built <see cref="ServiceBusSendMessageContext"/>.</returns>
    public Task<ServiceBusSendMessageContext> CreateRequestAsync(OutboundContext contextIn)
    {
        var message = new ServiceBusMessage(_serializer.Serialize(contextIn.Request));
        foreach (var header in contextIn.Headers)
        {
            message.ApplicationProperties[header.Key] = header.Value;
        }

        message.ApplicationProperties[_topicPropertyKey] = contextIn.Topic;

        return Task.FromResult(new ServiceBusSendMessageContext(message));
    }

    /// <summary>
    /// Marks the outbound context as accepted. Service Bus has no request/response semantics beyond a
    /// send acknowledgement.
    /// </summary>
    /// <param name="contextIn">The outbound context to set the response on.</param>
    /// <param name="contextOut">The completed <see cref="ServiceBusSendMessageContext"/>.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(OutboundContext contextIn, ServiceBusSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Accepted<Void>();
        return Task.CompletedTask;
    }
}
