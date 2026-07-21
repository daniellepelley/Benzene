using Benzene.CodeGen.Client;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Client;

public class CSharpTypeNameTest
{
    private readonly CSharpTypeName _typeName = new();

    [Fact]
    public void GetName_PlainObjectWithoutAdditionalProperties_DoesNotThrow()
    {
        // A "type": "object" schema with no additionalProperties leaves the property null; reading its
        // .Type used to throw a NullReferenceException.
        var schema = new OpenApiSchema { Type = "object" };

        Assert.Equal("object", _typeName.GetName(schema));
    }

    [Fact]
    public void GetName_StringMap_IsDictionaryOfString()
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            AdditionalProperties = new OpenApiSchema { Type = "string" }
        };

        Assert.Equal("Dictionary<string, string>", _typeName.GetName(schema));
    }

    [Fact]
    public void GetName_Int32_IsInt()
    {
        Assert.Equal("int", _typeName.GetName(new OpenApiSchema { Type = "integer" }));
        Assert.Equal("int", _typeName.GetName(new OpenApiSchema { Type = "integer", Format = "int32" }));
    }

    [Fact]
    public void GetName_Int64_IsLong_NotTruncatedToInt()
    {
        // An int64-format integer must be a long, or a generated client silently truncates 64-bit values.
        Assert.Equal("long", _typeName.GetName(new OpenApiSchema { Type = "integer", Format = "int64" }));
        Assert.Equal("long?", _typeName.GetName(new OpenApiSchema { Type = "integer", Format = "int64", Nullable = true }));
    }
}
