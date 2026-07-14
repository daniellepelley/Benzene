using System.Text.Json;
using System.Text.Json.Nodes;

namespace Benzene.Aws.Lambda.DynamoDb.TestHelpers;

/// <summary>
/// Marshals plain JSON into DynamoDB AttributeValue format — the reverse of
/// <see cref="DynamoDbAttributeValueConverter"/> — so tests can build realistic stream images from
/// ordinary objects: string → <c>S</c>, number → <c>N</c>, bool → <c>BOOL</c>, null → <c>NULL</c>,
/// object → <c>M</c>, array → <c>L</c>.
/// </summary>
public static class DynamoDbAttributeValueMarshaller
{
    /// <summary>
    /// Converts a plain JSON object into an AttributeValue map suitable for a stream record image.
    /// </summary>
    /// <param name="plainObject">The plain JSON object to marshal.</param>
    /// <returns>The AttributeValue-format JSON object.</returns>
    public static JsonObject ToAttributeValueMap(JsonObject plainObject)
    {
        var attributeMap = new JsonObject();
        foreach (var property in plainObject)
        {
            attributeMap[property.Key] = ToAttributeValue(property.Value);
        }

        return attributeMap;
    }

    private static JsonObject ToAttributeValue(JsonNode? value)
    {
        if (value == null)
        {
            return new JsonObject { ["NULL"] = true };
        }

        switch (value.GetValueKind())
        {
            case JsonValueKind.Object:
                return new JsonObject { ["M"] = ToAttributeValueMap(value.AsObject()) };
            case JsonValueKind.Array:
                var list = new JsonArray();
                foreach (var item in value.AsArray())
                {
                    list.Add(ToAttributeValue(item));
                }

                return new JsonObject { ["L"] = list };
            case JsonValueKind.String:
                return new JsonObject { ["S"] = value.GetValue<string>() };
            case JsonValueKind.True:
            case JsonValueKind.False:
                return new JsonObject { ["BOOL"] = value.GetValue<bool>() };
            case JsonValueKind.Number:
                return new JsonObject { ["N"] = value.ToJsonString() };
            default:
                return new JsonObject { ["NULL"] = true };
        }
    }
}
