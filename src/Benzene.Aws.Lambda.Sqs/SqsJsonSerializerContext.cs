using System.Text.Json.Serialization;
using Amazon.Lambda.SQSEvents;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// System.Text.Json source-generation context for the SQS event and partial-batch-response types, so
/// <see cref="SqsLambdaHandler"/> reads the event and writes its <see cref="SQSBatchResponse"/> without
/// System.Text.Json building that metadata by reflection on the first (cold) invocation. Public so an
/// app can reuse it (e.g. when moving the function toward trimming/Native AOT).
/// </summary>
[JsonSerializable(typeof(SQSEvent))]
[JsonSerializable(typeof(SQSBatchResponse))]
public partial class SqsJsonSerializerContext : JsonSerializerContext
{
}
