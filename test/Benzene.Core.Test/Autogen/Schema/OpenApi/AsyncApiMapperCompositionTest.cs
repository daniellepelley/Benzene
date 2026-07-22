using Benzene.Schema.OpenApi.AsyncApi;
using ByteBard.AsyncAPI.Models;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi;

/// <summary>
/// The AsyncAPI mapper must carry composition keywords (oneOf/allOf/anyOf, discriminator,
/// additionalProperties) through to the AsyncAPI schema model instead of silently dropping them.
/// </summary>
public class AsyncApiMapperCompositionTest
{
    private static global::Microsoft.OpenApi.Models.OpenApiSchema Ref(string id) =>
        new()
        {
            Reference = new global::Microsoft.OpenApi.Models.OpenApiReference
            {
                Id = id,
                Type = global::Microsoft.OpenApi.Models.ReferenceType.Schema,
            },
        };

    [Fact]
    public void Map_CarriesOneOfAndDiscriminator()
    {
        var input = new global::Microsoft.OpenApi.Models.OpenApiSchema
        {
            OneOf = { Ref("CardPayment"), Ref("BankPayment") },
            Discriminator = new global::Microsoft.OpenApi.Models.OpenApiDiscriminator
            {
                PropertyName = "kind",
            },
        };

        var mapped = Mapper.Map(input)!;

        Assert.Equal(2, mapped.OneOf.Count);
        Assert.All(mapped.OneOf, x => Assert.IsType<AsyncApiJsonSchemaReference>(x));
        Assert.Equal("kind", mapped.Discriminator);
    }

    [Fact]
    public void Map_CarriesAllOfAndOwnProperties()
    {
        var input = new global::Microsoft.OpenApi.Models.OpenApiSchema
        {
            Type = "object",
            AllOf = { Ref("PaymentMethod") },
            Properties =
            {
                ["cardNumber"] = new global::Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" },
            },
        };

        var mapped = Mapper.Map(input)!;

        Assert.Single(mapped.AllOf);
        Assert.IsType<AsyncApiJsonSchemaReference>(mapped.AllOf[0]);
        Assert.True(mapped.Properties.ContainsKey("cardNumber"));
    }

    [Fact]
    public void Map_CarriesAdditionalPropertiesAndAnyOf()
    {
        var input = new global::Microsoft.OpenApi.Models.OpenApiSchema
        {
            Type = "object",
            AnyOf = { new global::Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" } },
            AdditionalProperties = new global::Microsoft.OpenApi.Models.OpenApiSchema { Type = "integer" },
        };

        var mapped = Mapper.Map(input)!;

        Assert.Single(mapped.AnyOf);
        Assert.NotNull(mapped.AdditionalProperties);
        Assert.Equal(SchemaType.Integer, mapped.AdditionalProperties.Type);
    }
}
