using System.Text.Json.Serialization;

namespace Benzene.Aws.Lambda.Kinesis;

/// <summary>
/// System.Text.Json source-generation context for the Kinesis event and partial-batch-response types,
/// so <see cref="KinesisLambdaHandler"/> reads the event and writes its <see cref="KinesisBatchResponse"/>
/// without System.Text.Json building that metadata by reflection on the first (cold) invocation. Public
/// so an app can reuse it (e.g. toward trimming/Native AOT).
/// </summary>
[JsonSerializable(typeof(KinesisEvent))]
[JsonSerializable(typeof(KinesisBatchResponse))]
public partial class KinesisJsonSerializerContext : JsonSerializerContext
{
}
