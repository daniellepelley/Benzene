using System.Text.Json;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// Extracts the message body as plain JSON unmarshalled from the record's AttributeValue-format
/// image (plan decision DS3): <c>NewImage</c> when present, else <c>OldImage</c> (REMOVE events),
/// else <c>Keys</c> (KEYS_ONLY stream views) — the most complete state available.
/// </summary>
public class DynamoDbMessageBodyGetter : IMessageBodyGetter<DynamoDbRecordContext>
{
    /// <summary>
    /// Gets the plain-JSON body from the record's image.
    /// </summary>
    /// <param name="context">The DynamoDB record context to extract the body from.</param>
    /// <returns>The unmarshalled JSON object as a string, or null if the record carries no image at all.</returns>
    public string GetBody(DynamoDbRecordContext context)
    {
        var data = context.Record.Dynamodb;
        if (data == null)
        {
            return null;
        }

        var image = FirstObject(data.NewImage, data.OldImage, data.Keys);
        return image.HasValue ? DynamoDbAttributeValueConverter.ToJson(image.Value) : null;
    }

    private static JsonElement? FirstObject(params JsonElement[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (candidate.ValueKind == JsonValueKind.Object)
            {
                return candidate;
            }
        }

        return null;
    }
}
