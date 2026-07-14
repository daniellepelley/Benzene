using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Amazon.EventBridge.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients.Common;
using Benzene.Results;

namespace Benzene.Clients.Aws.EventBridge;

/// <summary>
/// Builds a single-entry <c>PutEvents</c> request from a Benzene client request: topic →
/// <c>DetailType</c>, serialized message → <c>Detail</c>. EventBridge has no native per-message
/// attributes, so Benzene wire headers (correlation, <c>traceparent</c>, ...) are embedded into
/// <c>Detail</c> under the reserved <c>_benzeneHeaders</c> key — the inbound
/// <c>Benzene.Aws.Lambda.EventBridge</c> binding lifts them back out. Embedding only happens when
/// there are headers to send and the payload serializes to a JSON object; extra field is benign for
/// non-Benzene consumers.
/// </summary>
public class EventBridgeContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, EventBridgeSendMessageContext>
{
    /// <summary>Must match the inbound binding's <c>EventBridgeMessageHeadersGetter.EmbeddedHeadersKey</c>.</summary>
    public const string EmbeddedHeadersKey = "_benzeneHeaders";

    private readonly ISerializer _serializer;
    private readonly string _source;
    private readonly string _eventBusName;

    public EventBridgeContextConverter(string source, string eventBusName = null)
        : this(source, eventBusName, new JsonSerializer())
    { }

    public EventBridgeContextConverter(string source, string eventBusName, ISerializer serializer)
    {
        _source = source;
        _eventBusName = eventBusName;
        _serializer = serializer;
    }

    public Task<EventBridgeSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Void> contextIn)
    {
        var entry = new PutEventsRequestEntry
        {
            Source = _source,
            DetailType = contextIn.Request.Topic,
            Detail = BuildDetail(contextIn.Request.Message, contextIn.Request.Headers)
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

    public Task MapResponseAsync(IBenzeneClientContext<T, Void> contextIn, EventBridgeSendMessageContext contextOut)
    {
        contextIn.Response = EventBridgeResultMapper.Map<Void>(contextOut.Response);
        return Task.CompletedTask;
    }

    private string BuildDetail(T message, IDictionary<string, string> headers)
    {
        var json = _serializer.Serialize(message);
        if (headers == null || headers.Count == 0)
        {
            return json;
        }

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

/// <summary>
/// Maps a <c>PutEvents</c> outcome to a Benzene result: a request can succeed at the HTTP level while
/// individual entries fail, so success requires both an OK status code and no failed entries.
/// </summary>
public static class EventBridgeResultMapper
{
    public static IBenzeneResult<TResponse> Map<TResponse>(PutEventsResponse response)
    {
        if (response == null)
        {
            return BenzeneResult.ServiceUnavailable<TResponse>("No response was received from EventBridge");
        }

        if (response.FailedEntryCount > 0)
        {
            var failed = response.Entries?.FirstOrDefault(x => !string.IsNullOrEmpty(x.ErrorCode));
            return BenzeneResult.ServiceUnavailable<TResponse>(
                failed != null ? $"{failed.ErrorCode}: {failed.ErrorMessage}" : "One or more events failed to publish");
        }

        return response.HttpStatusCode == System.Net.HttpStatusCode.OK
            ? BenzeneResult.Accepted<TResponse>()
            : BenzeneResultHttpMapper.Map<TResponse>(response.HttpStatusCode);
    }
}
