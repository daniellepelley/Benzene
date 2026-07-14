using System.Text.Json;
using System.Text.Json.Nodes;
using Benzene.Aws.Lambda.DynamoDb;
using Benzene.Aws.Lambda.DynamoDb.TestHelpers;
using Xunit;

namespace Benzene.Test.Aws.DynamoDb;

public class DynamoDbAttributeValueConverterTest
{
    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    [Fact]
    public void ToJson_UnmarshalsEveryDescriptorType()
    {
        var attributeMap = Parse(
            "{" +
            "\"Str\":{\"S\":\"hi\"}," +
            "\"Num\":{\"N\":\"1.5\"}," +
            "\"Flag\":{\"BOOL\":true}," +
            "\"Nothing\":{\"NULL\":true}," +
            "\"Nested\":{\"M\":{\"Inner\":{\"S\":\"x\"}}}," +
            "\"List\":{\"L\":[{\"S\":\"a\"},{\"N\":\"2\"}]}," +
            "\"Strings\":{\"SS\":[\"a\",\"b\"]}," +
            "\"Numbers\":{\"NS\":[\"1\",\"2.5\"]}," +
            "\"Blob\":{\"B\":\"AQID\"}" +
            "}");

        var result = DynamoDbAttributeValueConverter.ToJsonNode(attributeMap);

        var expected = JsonNode.Parse(
            "{" +
            "\"Str\":\"hi\"," +
            "\"Num\":1.5," +
            "\"Flag\":true," +
            "\"Nothing\":null," +
            "\"Nested\":{\"Inner\":\"x\"}," +
            "\"List\":[\"a\",2]," +
            "\"Strings\":[\"a\",\"b\"]," +
            "\"Numbers\":[1,2.5]," +
            "\"Blob\":\"AQID\"" +
            "}");
        Assert.True(JsonNode.DeepEquals(expected, result));
    }

    [Fact]
    public void ToJson_UnknownDescriptor_PassesRawValueThrough()
    {
        var attributeMap = Parse("{\"Weird\":{\"XX\":{\"foo\":1}}}");

        var result = DynamoDbAttributeValueConverter.ToJsonNode(attributeMap);

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("{\"Weird\":{\"foo\":1}}"), result));
    }

    [Fact]
    public void Marshaller_RoundTripsPlainJson()
    {
        var plain = (JsonObject)JsonNode.Parse(
            "{\"Name\":\"hi\",\"Count\":2,\"Ok\":true,\"Missing\":null,\"Nested\":{\"A\":\"b\"},\"List\":[1,\"x\"]}");

        var marshalled = DynamoDbAttributeValueMarshaller.ToAttributeValueMap(plain);
        var roundTripped = DynamoDbAttributeValueConverter.ToJsonNode(Parse(marshalled.ToJsonString()));

        Assert.True(JsonNode.DeepEquals(plain, roundTripped));
    }
}
