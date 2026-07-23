using System.Text.Json.Serialization;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// System.Text.Json source-generation context for the DynamoDB Streams event and partial-batch-response
/// types, so <see cref="DynamoDbLambdaHandler"/> reads the event and writes its
/// <see cref="DynamoDbBatchResponse"/> without System.Text.Json building that metadata by reflection on
/// the first (cold) invocation. Public so an app can reuse it (e.g. toward trimming/Native AOT).
/// </summary>
[JsonSerializable(typeof(DynamoDbEvent))]
[JsonSerializable(typeof(DynamoDbBatchResponse))]
public partial class DynamoDbJsonSerializerContext : JsonSerializerContext
{
}
