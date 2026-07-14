using System.Text.Json;
using System.Text.Json.Nodes;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// Unmarshals DynamoDB AttributeValue JSON (<c>{"Id": {"N": "101"}, "Message": {"S": "hi"}}</c>)
/// into plain JSON (<c>{"Id": 101, "Message": "hi"}</c>), so message handlers deserialize ordinary
/// POCOs instead of the stream's type-descriptor format (plan decision DS3).
/// </summary>
public static class DynamoDbAttributeValueConverter
{
    /// <summary>
    /// Converts an AttributeValue map (an item image or key set) to a plain JSON string.
    /// </summary>
    /// <param name="attributeMap">The AttributeValue map, e.g. a record's <c>NewImage</c>.</param>
    /// <returns>The plain JSON object as a string.</returns>
    public static string ToJson(JsonElement attributeMap)
    {
        return ToJsonNode(attributeMap).ToJsonString();
    }

    /// <summary>
    /// Converts an AttributeValue map (an item image or key set) to a plain <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="attributeMap">The AttributeValue map, e.g. a record's <c>NewImage</c>.</param>
    /// <returns>The plain JSON object.</returns>
    public static JsonObject ToJsonNode(JsonElement attributeMap)
    {
        var jsonObject = new JsonObject();
        foreach (var property in attributeMap.EnumerateObject())
        {
            jsonObject[property.Name] = ConvertValue(property.Value);
        }

        return jsonObject;
    }

    private static JsonNode ConvertValue(JsonElement attributeValue)
    {
        foreach (var property in attributeValue.EnumerateObject())
        {
            switch (property.Name)
            {
                case "S":
                case "B":
                    return JsonValue.Create(property.Value.GetString());
                case "N":
                    // DynamoDB numbers are strings on the wire but are valid JSON number literals.
                    return JsonNode.Parse(property.Value.GetString());
                case "BOOL":
                    return JsonValue.Create(property.Value.GetBoolean());
                case "NULL":
                    return null;
                case "M":
                    return ToJsonNode(property.Value);
                case "L":
                    return ConvertArray(property.Value, ConvertValue);
                case "SS":
                case "BS":
                    return ConvertArray(property.Value, item => JsonValue.Create(item.GetString()));
                case "NS":
                    return ConvertArray(property.Value, item => JsonNode.Parse(item.GetString()));
                default:
                    // Unknown descriptor — pass the raw value through rather than throwing, so a new
                    // DynamoDB type doesn't break existing consumers.
                    return JsonNode.Parse(property.Value.GetRawText());
            }
        }

        return null;
    }

    private static JsonArray ConvertArray(JsonElement array, System.Func<JsonElement, JsonNode> convertItem)
    {
        var jsonArray = new JsonArray();
        foreach (var item in array.EnumerateArray())
        {
            jsonArray.Add(convertItem(item));
        }

        return jsonArray;
    }
}
