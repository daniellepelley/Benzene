using System.Text.Json.Serialization;

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// System.Text.Json source-generation context for the EventBridge event type, so
/// <see cref="EventBridgeLambdaHandler"/> reads the event without System.Text.Json building that
/// metadata by reflection on the first (cold) invocation. EventBridge targets are invoked
/// asynchronously (no response is written), so only the event type is registered. Public so an app can
/// reuse it (e.g. toward trimming/Native AOT).
/// </summary>
[JsonSerializable(typeof(EventBridgeEvent))]
public partial class EventBridgeJsonSerializerContext : JsonSerializerContext
{
}
