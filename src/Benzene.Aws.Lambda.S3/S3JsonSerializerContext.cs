using System.Text.Json.Serialization;
using Amazon.Lambda.S3Events;

namespace Benzene.Aws.Lambda.S3;

/// <summary>
/// System.Text.Json source-generation context for the S3 event type, so <see cref="S3LambdaHandler"/>
/// reads the event without System.Text.Json building that metadata by reflection on the first (cold)
/// invocation. S3 notifications are fire-and-forget (no response is written), so only the event type is
/// registered. Public so an app can reuse it (e.g. toward trimming/Native AOT).
/// </summary>
[JsonSerializable(typeof(S3Event))]
public partial class S3JsonSerializerContext : JsonSerializerContext
{
}
