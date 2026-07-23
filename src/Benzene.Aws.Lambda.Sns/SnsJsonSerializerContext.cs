using System.Text.Json.Serialization;
using Amazon.Lambda.SNSEvents;

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// System.Text.Json source-generation context for the SNS event type, so <see cref="SnsLambdaHandler"/>
/// reads the event without System.Text.Json building that metadata by reflection on the first (cold)
/// invocation. SNS delivery is fire-and-forget (no response is written), so only the event type is
/// registered. Public so an app can reuse it (e.g. toward trimming/Native AOT).
/// </summary>
[JsonSerializable(typeof(SNSEvent))]
public partial class SnsJsonSerializerContext : JsonSerializerContext
{
}
