using System.Text.Json;
using System.Text.Json.Nodes;
using Benzene.Abstractions;
using JsonSerializer = Benzene.Core.MessageHandlers.Serialization.JsonSerializer;

namespace Benzene.Aws.Lambda.DynamoDb.TestHelpers;

public static class MessageBuilderExtensions
{
    /// <summary>
    /// Builds a realistic DynamoDB Streams event from the message builder. The topic is parsed as
    /// <c>"tableName:EVENT_NAME"</c> (a bare table name defaults to <c>INSERT</c>) — matching the
    /// <c>"{tableName}:{eventName}"</c> topics the adapter routes on — and the message is marshalled
    /// into AttributeValue format and placed in <c>NewImage</c> (or <c>OldImage</c> for
    /// <c>REMOVE</c> events).
    /// </summary>
    /// <remarks>
    /// Drive the event through <c>DynamoDbApplication</c> directly (or an
    /// <c>AwsEventStreamContext</c> pipeline serialized with the Lambda System.Text.Json
    /// serializer). The Newtonsoft-based <c>AwsLambdaBenzeneTestHost.SendEventAsync(object)</c>
    /// path cannot round-trip the raw <c>JsonElement</c> images this event carries.
    /// </remarks>
    public static DynamoDbEvent AsDynamoDb<T>(this IMessageBuilder<T> source, int numberOfRecords = 1)
    {
        var topicParts = (source.Topic ?? "benzene-test").Split(':');
        var tableName = topicParts[0];
        var eventName = topicParts.Length > 1 ? topicParts[1] : "INSERT";

        var json = new JsonSerializer().Serialize(source.Message);
        var image = JsonNode.Parse(json) is JsonObject plainObject
            ? DynamoDbAttributeValueMarshaller.ToAttributeValueMap(plainObject)
            : new JsonObject();

        using var document = JsonDocument.Parse(image.ToJsonString());
        var imageElement = document.RootElement.Clone();

        return new DynamoDbEvent
        {
            Records = Enumerable.Range(1, numberOfRecords).Select(sequence =>
                new DynamoDbStreamRecord
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventName = eventName,
                    EventVersion = "1.1",
                    EventSource = "aws:dynamodb",
                    EventSourceArn = $"arn:aws:dynamodb:eu-west-1:123456789012:table/{tableName}/stream/2026-01-01T00:00:00.000",
                    AwsRegion = "eu-west-1",
                    Dynamodb = new DynamoDbStreamData
                    {
                        NewImage = eventName == "REMOVE" ? null : (JsonElement?)imageElement,
                        OldImage = eventName == "REMOVE" ? imageElement : (JsonElement?)null,
                        SequenceNumber = sequence.ToString(),
                        StreamViewType = "NEW_AND_OLD_IMAGES"
                    }
                }
            ).ToList()
        };
    }
}
