using System;
using System.Collections.Generic;
using System.IO;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.Examples;
using Benzene.Test.Autogen.CodeGen.Model;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi.Examples;

public class ExamplePayloadBuilderTest
{
    private string Load(string fileName) => File.ReadAllText($"Autogen/Schema/OpenApi/Examples/{fileName}.json").Replace(Environment.NewLine, string.Empty);

    [Fact]
    public void CreateTenantMessage_Test()
    {
        var expected = Load("CreateTenantMessage");

        var schema = Create(typeof(CreateTenantMessage));

        var examplePayloadBuilder = new ExamplePayloadBuilder();
        var actual = examplePayloadBuilder.BuildAsJson(schema["CreateTenantMessage"], new SchemaGetter(schema));

        Assert.Equal(expected.TrimEnd(), actual.TrimEnd());
    }

    [Fact]
    public void CreateClientMessage_Test()
    {
        var expected = Load("CreateClientMessage");

        var schema = Create(typeof(CreateClientMessage));

        var examplePayloadBuilder = new ExamplePayloadBuilder();
        var actual = examplePayloadBuilder.BuildAsJson(schema["CreateClientMessage"], new SchemaGetter(schema));

        Assert.Equal(expected.TrimEnd(), actual.TrimEnd());
    }

    [Fact]
    public void CreateUserMessage_Test()
    {
        var expected = Load("CreateUserMessage");

        var schema = Create(typeof(CreateUserMessage));

        var examplePayloadBuilder = new ExamplePayloadBuilder();
        var actual = examplePayloadBuilder.BuildAsJson(schema["CreateUserMessage"], new SchemaGetter(schema));

        Assert.Equal(expected.TrimEnd(), actual.TrimEnd());
    }

    [Fact]
    public void UserDto_Test()
    {
        var expected = Load("UserDto");

        var schema = Create(typeof(UserDto));

        var examplePayloadBuilder = new ExamplePayloadBuilder();
        var actual = examplePayloadBuilder.BuildAsJson(schema["UserDto"], new SchemaGetter(schema));

        Assert.Equal(expected.TrimEnd(), actual.TrimEnd());
    }

    [Fact]
    public void AllFieldTypesMessage_Test()
    {
        var expected = Load("AllFieldTypesMessage");

        var schema = Create(typeof(AllFieldTypesMessage));

        var examplePayloadBuilder = new ExamplePayloadBuilder();
        var actual = examplePayloadBuilder.BuildAsJson(schema["AllFieldTypesMessage"], new SchemaGetter(schema));

        Assert.Equal(expected.TrimEnd(), actual.TrimEnd());
    }

    [Fact]
    public void Enum_FirstValueIsUsed()
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties =
            {
                ["Priority"] = new OpenApiSchema
                {
                    Type = "string",
                    Enum = { new OpenApiString("low"), new OpenApiString("standard"), new OpenApiString("high") }
                }
            }
        };

        var actual = new ExamplePayloadBuilder().BuildAsJson(schema, EmptyGetter());

        Assert.Equal("{\"priority\":\"low\"}", actual);
    }

    [Fact]
    public void Example_TakesPrecedenceOverDefaultAndEnum()
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties =
            {
                ["Priority"] = new OpenApiSchema
                {
                    Type = "string",
                    Example = new OpenApiString("from-example"),
                    Default = new OpenApiString("from-default"),
                    Enum = { new OpenApiString("low") }
                }
            }
        };

        var actual = new ExamplePayloadBuilder().BuildAsJson(schema, EmptyGetter());

        Assert.Equal("{\"priority\":\"from-example\"}", actual);
    }

    [Fact]
    public void Default_TakesPrecedenceOverEnum()
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties =
            {
                ["Priority"] = new OpenApiSchema
                {
                    Type = "string",
                    Default = new OpenApiString("from-default"),
                    Enum = { new OpenApiString("low") }
                }
            }
        };

        var actual = new ExamplePayloadBuilder().BuildAsJson(schema, EmptyGetter());

        Assert.Equal("{\"priority\":\"from-default\"}", actual);
    }

    [Fact]
    public void Integer_ClampedIntoMinimumMaximumRange()
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties =
            {
                ["AtLeastOneHundred"] = new OpenApiSchema { Type = "integer", Minimum = 100 },
                ["AtMostTen"] = new OpenApiSchema { Type = "integer", Maximum = 10 },
                ["Unconstrained"] = new OpenApiSchema { Type = "integer" }
            }
        };

        var actual = new ExamplePayloadBuilder().BuildAsJson(schema, EmptyGetter());

        Assert.Equal("{\"atLeastOneHundred\":100,\"atMostTen\":10,\"unconstrained\":42}", actual);
    }

    [Fact]
    public void String_SizedWithinLengthConstraints()
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties =
            {
                ["Long"] = new OpenApiSchema { Type = "string", MinLength = 10 },
                ["Short"] = new OpenApiSchema { Type = "string", MaxLength = 3 },
                ["Unconstrained"] = new OpenApiSchema { Type = "string" }
            }
        };

        var actual = new ExamplePayloadBuilder().BuildAsJson(schema, EmptyGetter());

        Assert.Equal("{\"long\":\"valuevalue\",\"short\":\"val\",\"unconstrained\":\"value\"}", actual);
    }

    [Fact]
    public void StringFormats_ProduceFormatSpecificValues()
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties =
            {
                ["Email"] = new OpenApiSchema { Type = "string", Format = "email" },
                ["Uri"] = new OpenApiSchema { Type = "string", Format = "uri" },
                ["Date"] = new OpenApiSchema { Type = "string", Format = "date" }
            }
        };

        var actual = new ExamplePayloadBuilder().BuildAsJson(schema, EmptyGetter());

        Assert.Equal("{\"email\":\"user@example.com\",\"uri\":\"https://example.com\",\"date\":\"2023-01-01\"}", actual);
    }

    [Fact]
    public void MutuallyRecursiveSchemas_TerminateWithEmptyObject()
    {
        var schemas = new Dictionary<string, OpenApiSchema>
        {
            ["A"] = new()
            {
                Type = "object",
                Properties = { ["B"] = Ref("B") }
            },
            ["B"] = new()
            {
                Type = "object",
                Properties = { ["A"] = Ref("A") }
            }
        };

        var actual = new ExamplePayloadBuilder().BuildAsJson(Ref("A"), new SchemaGetter(schemas));

        Assert.Equal("{\"b\":{\"a\":{}}}", actual);
    }

    [Fact]
    public void DeepReferenceChain_ExpandsFully()
    {
        var schemas = new Dictionary<string, OpenApiSchema>
        {
            ["A"] = new()
            {
                Type = "object",
                Properties = { ["B"] = Ref("B") }
            },
            ["B"] = new()
            {
                Type = "object",
                Properties = { ["C"] = Ref("C") }
            },
            ["C"] = new()
            {
                Type = "object",
                Properties = { ["Name"] = new OpenApiSchema { Type = "string" } }
            }
        };

        var actual = new ExamplePayloadBuilder().BuildAsJson(Ref("A"), new SchemaGetter(schemas));

        Assert.Equal("{\"b\":{\"c\":{\"name\":\"value\"}}}", actual);
    }

    [Fact]
    public void KnownValues_PathKeyWinsOverBareNameKey()
    {
        var knownValues = new Dictionary<string, object>
        {
            { "internal.value1", "from-path" },
            { "value1", "from-bare-name" },
            { "id", "known-id" }
        };

        var actual = new ExamplePayloadBuilder(knownValues).Build(typeof(UserDto));

        Assert.Equal("known-id", actual["id"]);
        var inner = (IDictionary<string, object>)actual["internal"];
        Assert.Equal("from-path", inner["value1"]);
    }

    private static ISchemaGetter EmptyGetter()
    {
        return new SchemaGetter(new Dictionary<string, OpenApiSchema>());
    }

    private static OpenApiSchema Ref(string id)
    {
        return new OpenApiSchema
        {
            Reference = new OpenApiReference { Id = id, Type = ReferenceType.Schema }
        };
    }

    public Dictionary<string, OpenApiSchema> Create(params Type[] types)
    {
        var schemaBuilder = new SchemaBuilder();

        foreach (var type in types)
        {
            schemaBuilder.AddSchema(type);
        }

        return schemaBuilder.Build();
    }
}
