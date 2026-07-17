using System.Collections.Generic;
using System.Text.Json;
using Benzene.Core.Versioning.Deserializer;
using Benzene.Core.Versioning.Schemas;
using Xunit;
using V1 = Benzene.Test.Core.Versioning.Schemas.V1;
using V2 = Benzene.Test.Core.Versioning.Schemas.V2;

namespace Benzene.Test.Core.Versioning;

public class PayloadDeserializerTest
{
    private class HeaderPayloadFields : IPayloadFields
    {
        public string GetSchemaVersion(JsonElement element) => element.GetProperty("schemaVersion").GetString();

        public string GetTopic(JsonElement element) => element.GetProperty("topic").GetString();
    }

    private static PayloadDeserializer CreateDeserializer()
    {
        var schemaCasters = new SchemaCasters(new SchemaCastersBuilder()
            .Add<V1.OrderPayload, V2.OrderPayload>("orderCreated", "V1", "V2", x => x.RegisterInitValue(o => o.Currency, "USD"))
            .Build());

        return new PayloadDeserializer(
            schemaCasters,
            new PayloadSchemaVersionLookUp(new Dictionary<string, string> { ["orderCreated"] = "V2" }),
            new HeaderPayloadFields(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    [Fact]
    public void Deserialize_SameVersion_DeserializesDirectly()
    {
        var json = JsonDocument.Parse(
            """{ "topic": "orderCreated", "schemaVersion": "V2", "id": "order-1", "quantity": 4, "currency": "GBP" }""").RootElement;

        var result = CreateDeserializer().Deserialize<V2.OrderPayload>(json);

        Assert.Equal("order-1", result.Id);
        Assert.Equal(4, result.Quantity);
        Assert.Equal("GBP", result.Currency);
    }

    [Fact]
    public void Deserialize_OlderVersion_UpcastsToTargetSchema()
    {
        var json = JsonDocument.Parse(
            """{ "topic": "orderCreated", "schemaVersion": "V1", "id": "order-1", "quantity": 4, "customerName": "Jo" }""").RootElement;

        var result = CreateDeserializer().Deserialize<V2.OrderPayload>(json);

        Assert.Equal("order-1", result.Id);
        Assert.Equal(4, result.Quantity);
        Assert.Equal("USD", result.Currency);
    }
}
