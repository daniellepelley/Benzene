using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Amazon.EventBridge.Model;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.EventBridge;

/// <summary>
/// Converts between an outbound <see cref="OutboundContext"/> and an
/// <see cref="EventBridgeSendMessageContext"/>, so an outbound route (<c>OutboundRoutingBuilder.Route</c>)
/// can publish via EventBridge. The <see cref="OutboundContext"/> counterpart of
/// <see cref="EventBridgeContextConverter{T}"/> — the SNS/SQS <c>OutboundContext</c> converters' EventBridge
/// twin. See <c>work/benzene-clients-redesign-plan.md</c> §3.
/// </summary>
/// <remarks>
/// EventBridge routes on the event's <c>Source</c>/<c>DetailType</c>, so the Benzene routing topic maps to
/// <c>DetailType</c> (what the inbound <c>Benzene.Aws.Lambda.EventBridge</c> binding reads back as the
/// topic) and the configured <c>source</c> maps to <c>Source</c>. EventBridge has no native per-message
/// attributes, so Benzene wire headers (correlation, <c>traceparent</c>, …) are embedded into
/// <c>Detail</c> under the reserved <c>_benzeneHeaders</c> key, exactly as
/// <see cref="EventBridgeContextConverter{T}"/> does — the inbound binding lifts them back out. Like every
/// other AWS sender the outbound path is fire-and-acknowledge, so the response is always
/// <see cref="IBenzeneResult{Void}"/> — a topic routed here must be sent via
/// <c>IBenzeneMessageSender.SendAsync&lt;TRequest,Void&gt;</c>.
/// </remarks>
public class OutboundEventBridgeContextConverter : IContextConverter<OutboundContext, EventBridgeSendMessageContext>
{
    /// <summary>Must match the inbound binding's <c>EventBridgeMessageHeadersGetter.EmbeddedHeadersKey</c>.</summary>
    public const string EmbeddedHeadersKey = EventBridgeContextConverter<object>.EmbeddedHeadersKey;

    private readonly ISerializer _serializer;
    private readonly string _source;
    private readonly string _eventBusName;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundEventBridgeContextConverter"/> class, using the
    /// default JSON serializer.
    /// </summary>
    /// <param name="source">The EventBridge event <c>source</c> stamped on published events.</param>
    /// <param name="eventBusName">The event bus to publish to; null/empty targets the default bus.</param>
    public OutboundEventBridgeContextConverter(string source, string eventBusName = null)
        : this(source, eventBusName, new JsonSerializer())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundEventBridgeContextConverter"/> class.
    /// </summary>
    /// <param name="source">The EventBridge event <c>source</c> stamped on published events.</param>
    /// <param name="eventBusName">The event bus to publish to; null/empty targets the default bus.</param>
    /// <param name="serializer">The serializer used to serialize the message payload into <c>Detail</c>.</param>
    public OutboundEventBridgeContextConverter(string source, string eventBusName, ISerializer serializer)
    {
        _source = source;
        _eventBusName = eventBusName;
        _serializer = serializer;
    }

    /// <summary>
    /// Builds a single-entry <c>PutEvents</c> request from the outbound context: topic → <c>DetailType</c>,
    /// serialized request → <c>Detail</c> (with headers embedded under <see cref="EmbeddedHeadersKey"/>).
    /// </summary>
    /// <param name="contextIn">The outbound context to convert.</param>
    /// <returns>A task that resolves to the EventBridge send message context.</returns>
    public Task<EventBridgeSendMessageContext> CreateRequestAsync(OutboundContext contextIn)
    {
        var entry = new PutEventsRequestEntry
        {
            Source = _source,
            DetailType = contextIn.Topic,
            Detail = BuildDetail(contextIn.Request, contextIn.Headers)
        };

        if (!string.IsNullOrEmpty(_eventBusName))
        {
            entry.EventBusName = _eventBusName;
        }

        return Task.FromResult(new EventBridgeSendMessageContext(new PutEventsRequest
        {
            Entries = new List<PutEventsRequestEntry> { entry }
        }));
    }

    /// <summary>
    /// Maps the <c>PutEvents</c> outcome back onto the outbound context as an
    /// <see cref="IBenzeneResult{Void}"/> (a request-level OK with no failed entries is success).
    /// </summary>
    /// <param name="contextIn">The outbound context to update with the result.</param>
    /// <param name="contextOut">The EventBridge send message context containing the response.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(OutboundContext contextIn, EventBridgeSendMessageContext contextOut)
    {
        contextIn.Response = EventBridgeResultMapper.Map<Void>(contextOut.Response);
        return Task.CompletedTask;
    }

    private string BuildDetail(object message, IDictionary<string, string> headers)
    {
        var json = _serializer.Serialize(message);
        if (headers == null || headers.Count == 0)
        {
            return json;
        }

        // Embedding only works when the payload is a JSON object; leave a non-object payload as-is.
        if (JsonNode.Parse(json) is not JsonObject detail)
        {
            return json;
        }

        var embedded = new JsonObject();
        foreach (var header in headers)
        {
            embedded[header.Key] = header.Value;
        }

        detail[EmbeddedHeadersKey] = embedded;
        return detail.ToJsonString();
    }
}
