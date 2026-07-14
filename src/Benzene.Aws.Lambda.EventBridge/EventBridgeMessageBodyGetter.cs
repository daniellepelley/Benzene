using System.Text.Json;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// The message body is the raw JSON text of the event's <c>detail</c> — the domain payload
/// (plan decision E3). The reserved <c>_benzeneHeaders</c> key, when present, is an extra field the
/// request mapper's deserialization simply ignores.
/// </summary>
public class EventBridgeMessageBodyGetter : IMessageBodyGetter<EventBridgeContext>
{
    public string GetBody(EventBridgeContext context)
    {
        var detail = context.Event.Detail;
        return detail.ValueKind == JsonValueKind.Undefined ? null : detail.GetRawText();
    }
}
