using System.Text.Json.Serialization;
using Amazon.Lambda.KafkaEvents;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// System.Text.Json source-generation context for the Kafka event and partial-batch-response types, so
/// <see cref="KafkaLambdaHandler"/> reads the event and writes its <see cref="KafkaBatchResponse"/>
/// without System.Text.Json building that metadata by reflection on the first (cold) invocation. Public
/// so an app can reuse it (e.g. toward trimming/Native AOT).
/// </summary>
[JsonSerializable(typeof(KafkaEvent))]
[JsonSerializable(typeof(KafkaBatchResponse))]
public partial class KafkaJsonSerializerContext : JsonSerializerContext
{
}
