using Benzene.Schema.OpenApi;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi;

public class SuppliedSchemaBuilderTest
{
    private const string ExampleSchemaJson = /*lang=json*/ """
        {
          "type": "object",
          "properties": {
            "id": { "type": "integer" },
            "name": { "type": "string", "maxLength": 5 },
            "address": { "$ref": "#/components/schemas/SuppliedAddress" }
          },
          "required": [ "name" ]
        }
        """;

    private const string AddressSchemaJson = /*lang=json*/ """
        {
          "type": "object",
          "properties": {
            "line1": { "type": "string" }
          }
        }
        """;

    private static SuppliedSchemaCatalog CreateCatalog()
    {
        return new SuppliedSchemaCatalog()
            .AddJson(typeof(ExampleRequestPayload), "SuppliedExample", ExampleSchemaJson)
            .AddJson(typeof(object), "SuppliedAddress", AddressSchemaJson);
    }

    [Fact]
    public void MappedType_ServesSuppliedSchema_AndRegistersWholeCatalog()
    {
        var builder = new SuppliedSchemaBuilder(CreateCatalog(), new SchemaBuilder());

        var reference = builder.AddSchema(typeof(ExampleRequestPayload));
        var schemas = builder.Build();

        Assert.Equal("SuppliedExample", reference.Reference?.Id);
        Assert.True(schemas.ContainsKey("SuppliedExample"));
        // The cross-$ref'd schema came along with the catalog, so the reference resolves.
        Assert.True(schemas.ContainsKey("SuppliedAddress"));
        Assert.Equal(5, schemas["SuppliedExample"].Properties["name"].MaxLength);
    }

    [Fact]
    public void UnmappedType_FallsBackToReflection()
    {
        var builder = new SuppliedSchemaBuilder(CreateCatalog(), new SchemaBuilder());

        builder.AddSchema(typeof(ExampleResponsePayload));
        var schemas = builder.Build();

        Assert.True(schemas.ContainsKey(nameof(ExampleResponsePayload)));
        // Nothing supplied was used, so the catalog is not dragged into the document.
        Assert.False(schemas.ContainsKey("SuppliedExample"));
    }

    [Fact]
    public void ComponentsJson_MapsListedTypes()
    {
        var catalog = new SuppliedSchemaCatalog().AddComponentsJson(
            /*lang=json*/ """
            {
              "SuppliedExample": { "type": "object", "properties": { "name": { "type": "string" } } },
              "SuppliedAddress": { "type": "object" }
            }
            """,
            new System.Collections.Generic.Dictionary<System.Type, string>
            {
                [typeof(ExampleRequestPayload)] = "SuppliedExample",
            });

        Assert.True(catalog.TryGetSchemaId(typeof(ExampleRequestPayload), out var schemaId));
        Assert.Equal("SuppliedExample", schemaId);
        Assert.Equal(2, catalog.SchemasById.Count);
    }
}
