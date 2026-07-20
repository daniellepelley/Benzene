using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

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
    private readonly ServiceBusSenderProperties? _senderProperties;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusContextConverter{T}"/> class using a
    /// <see cref="JsonSerializer"/> to serialize the outgoing message.
    /// </summary>
    /// <param name="topicPropertyKey">The application property the topic is written to (defaults to <see cref="DefaultTopicProperty"/>).</param>
    /// <param name="senderProperties">Optional mapping of headers onto broker-level message properties (MessageId, SessionId, ScheduledEnqueueTime, TimeToLive).</param>
    public ServiceBusContextConverter(string topicPropertyKey = DefaultTopicProperty, ServiceBusSenderProperties? senderProperties = null)
        : this(new JsonSerializer(), topicPropertyKey, senderProperties)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusContextConverter{T}"/> class.
    /// </summary>
    /// <param name="serializer">The serializer used to serialize the outgoing message.</param>
    /// <param name="topicPropertyKey">The application property the topic is written to (defaults to <see cref="DefaultTopicProperty"/>).</param>
    /// <param name="senderProperties">Optional mapping of headers onto broker-level message properties (MessageId, SessionId, ScheduledEnqueueTime, TimeToLive).</param>
    public ServiceBusContextConverter(ISerializer serializer, string topicPropertyKey = DefaultTopicProperty, ServiceBusSenderProperties? senderProperties = null)
    {
        _serializer = serializer;
        _topicPropertyKey = topicPropertyKey;
        _senderProperties = senderProperties;
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

        ApplySenderProperties(message, contextIn.Request.Headers);

        return Task.FromResult(new ServiceBusSendMessageContext(message));
    }

    private void ApplySenderProperties(ServiceBusMessage message, IDictionary<string, string> headers)
    {
        if (_senderProperties == null)
        {
            return;
        }

        if (TryGetHeader(headers, _senderProperties.MessageIdHeader, out var messageId))
        {
            message.MessageId = messageId;
        }

        if (TryGetHeader(headers, _senderProperties.SessionIdHeader, out var sessionId))
        {
            message.SessionId = sessionId;
        }

        if (TryGetHeader(headers, _senderProperties.ScheduledEnqueueTimeHeader, out var scheduled) &&
            DateTimeOffset.TryParse(scheduled, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var when))
        {
            message.ScheduledEnqueueTime = when;
        }

        if (TryGetHeader(headers, _senderProperties.TimeToLiveHeader, out var ttl) && TryParseTimeToLive(ttl, out var timeToLive))
        {
            message.TimeToLive = timeToLive;
        }
    }

    private static bool TryGetHeader(IDictionary<string, string> headers, string? key, out string value)
    {
        if (!string.IsNullOrEmpty(key) && headers.TryGetValue(key, out var found) && !string.IsNullOrEmpty(found))
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryParseTimeToLive(string value, out TimeSpan timeToLive)
    {
        // A plain number is treated as seconds; otherwise an ISO-8601 duration (PT30S) or a TimeSpan
        // string (00:00:30).
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var seconds))
        {
            timeToLive = TimeSpan.FromSeconds(seconds);
            return true;
        }

        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out timeToLive))
        {
            return true;
        }

        try
        {
            timeToLive = XmlConvert.ToTimeSpan(value);
            return true;
        }
        catch (FormatException)
        {
            timeToLive = default;
            return false;
        }
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
