using System.Collections.Generic;
using System.Text.Json;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// Maps the event onto Benzene headers (plan decision E4): envelope metadata under
/// <c>eventbridge-</c>-prefixed keys, plus any Benzene wire headers (correlation, <c>traceparent</c>, ...)
/// lifted verbatim from the reserved <c>_benzeneHeaders</c> object inside <c>detail</c> — EventBridge has
/// no native per-message attributes, so that's where the outbound client embeds them.
/// </summary>
public class EventBridgeMessageHeadersGetter : IMessageHeadersGetter<EventBridgeContext>
{
    /// <summary>The reserved key inside <c>detail</c> that carries embedded Benzene wire headers.</summary>
    public const string EmbeddedHeadersKey = "_benzeneHeaders";

    public IDictionary<string, string> GetHeaders(EventBridgeContext context)
    {
        var headers = new Dictionary<string, string>();
        var @event = context.Event;

        AddIfPresent(headers, "eventbridge-id", @event.Id);
        AddIfPresent(headers, "eventbridge-source", @event.Source);
        AddIfPresent(headers, "eventbridge-account", @event.Account);
        AddIfPresent(headers, "eventbridge-region", @event.Region);
        AddIfPresent(headers, "eventbridge-time", @event.Time);
        AddIfPresent(headers, "eventbridge-detail-type", @event.DetailType);

        if (@event.Detail.ValueKind == JsonValueKind.Object &&
            @event.Detail.TryGetProperty(EmbeddedHeadersKey, out var embedded) &&
            embedded.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in embedded.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    headers[property.Name] = property.Value.GetString();
                }
            }
        }

        return headers;
    }

    private static void AddIfPresent(IDictionary<string, string> headers, string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            headers[key] = value;
        }
    }
}
