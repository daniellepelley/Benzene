using System;
using System.Collections.Generic;
using System.IO;
using Benzene.CodeGen.Core;
using Benzene.Schema.OpenApi;
using Benzene.Test.Autogen.CodeGen.Model;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Core;

public class PayloadBuilderTest
{
    private string Load(string fileName) => File.ReadAllText($"Autogen/CodeGen/Core/Examples/{fileName}.json").Replace(Environment.NewLine, string.Empty);

    [Fact]
    public void CreateTenantMessage_Test()
    {
        var expected = Load("CreateTenantMessage");
        
        var schema = Create(typeof(CreateTenantMessage));
        
        var jsonPayloadBuilder = new PayloadBuilder();
        var actual = jsonPayloadBuilder.BuildAsJson(schema["CreateTenantMessage"], new SchemaGetter(schema));

        Assert.Equal(expected.TrimEnd(), actual.TrimEnd());
    }

    [Fact]
    public void CreateClientMessage_Test()
    {
        var expected = Load("CreateClientMessage");
        
        var schema = Create(typeof(CreateClientMessage));
        
        var jsonPayloadBuilder = new PayloadBuilder();
        var actual = jsonPayloadBuilder.BuildAsJson(schema["CreateClientMessage"], new SchemaGetter(schema));

        Assert.Equal(expected.TrimEnd(), actual.TrimEnd());
    }

    [Fact]
    public void CreateUserMessage_Test()
    {
        var expected = Load("CreateUserMessage");
        
        var schema = Create(typeof(CreateUserMessage));
        
        var jsonPayloadBuilder = new PayloadBuilder();
        var actual = jsonPayloadBuilder.BuildAsJson(schema["CreateUserMessage"], new SchemaGetter(schema));

        Assert.Equal(expected.TrimEnd(), actual.TrimEnd());
    }

    [Fact]
    public void UserDto_Test()
    {
        var expected = Load("UserDto");

        var schema = Create(typeof(UserDto));

        var jsonPayloadBuilder = new PayloadBuilder();
        var actual = jsonPayloadBuilder.BuildAsJson(schema["UserDto"], new SchemaGetter(schema));

        Assert.Equal(expected.TrimEnd(), actual.TrimEnd());
    }

    [Fact]
    public void AllFieldTypesMessage_Test()
    {
        var expected = Load("AllFieldTypesMessage");
        
        var schema = Create(typeof(AllFieldTypesMessage));
        
        var jsonPayloadBuilder = new PayloadBuilder();
        var actual = jsonPayloadBuilder.BuildAsJson(schema["AllFieldTypesMessage"], new SchemaGetter(schema));

        Assert.Equal(expected.TrimEnd(), actual.TrimEnd());
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
