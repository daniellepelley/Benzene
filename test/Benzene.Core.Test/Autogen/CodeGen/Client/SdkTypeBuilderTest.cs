using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Benzene.CodeGen.Client;
using Benzene.Schema.OpenApi;
using Benzene.Test.Autogen.CodeGen.Helpers;
using Benzene.Test.Autogen.CodeGen.Model;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Client;

public class SdkTypeBuilderTest
{
    private const string BaseNameSpace = "Platform.Service.Clients.User";

    private string LoadExpected(string fileName) =>
        File.ReadAllLines($"{Directory.GetCurrentDirectory()}/Autogen/CodeGen/Client/Examples/{fileName}.txt").ToText();

    [Fact]
    public void BuildType_Simple_Test()
    {
        var lambdaServiceSdkBuilder = new OpenApiSchemaCSharpTypeBuilder(BaseNameSpace);

        var schemaBuilder = new SchemaBuilder();
        schemaBuilder.AddSchema(typeof(MessageWrapper<GetUserMessage>));

        var result = lambdaServiceSdkBuilder.BuildType(schemaBuilder.Build().First());

        var expectedMessageWrapper = LoadExpected("LambdaService_GetUserMessage");
        Assert.Equal("GetUserMessage.cs", result.Name);
        Assert.Equal(expectedMessageWrapper, result.Lines.ToText());
    }


    [Fact]
    public void Build_MessageWrapper_Test()
    {
        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "user:get", (typeof(GetUserMessage), typeof(MessageWrapper<GetUserMessage>), typeof(GetUserMessage)) }
        };

        var lambdaServiceSdkBuilder = new OpenApiSchemaCSharpTypeBuilder(BaseNameSpace);

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToOpenApiSchemas());

        var expectedMessageWrapper = LoadExpected("LambdaService_GetUserMessageMessageWrapper");
        var expectedGetUserMessage = LoadExpected("LambdaService_GetUserMessage");
        Assert.Equal(expectedMessageWrapper, result["GetUserMessageMessageWrapper.cs"]);
        Assert.Equal(expectedGetUserMessage, result["GetUserMessage.cs"]);
    }

    [Fact]
    public void Build_HasDictionary_Test()
    {
        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "user:get", (typeof(GetUserMessage), typeof(MessageWrapper<GetUserMessage>), typeof(GetUserMessage)) }
        };

        var lambdaServiceSdkBuilder = new OpenApiSchemaCSharpTypeBuilder(BaseNameSpace);

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToOpenApiSchemas());

        var expectedMessageWrapper = LoadExpected("LambdaService_GetUserMessageMessageWrapper");
        var expectedGetUserMessage = LoadExpected("LambdaService_GetUserMessage");

        Assert.Equal(2, result.Count);
        Assert.Equal(expectedMessageWrapper, result["GetUserMessageMessageWrapper.cs"]);
        Assert.Equal(expectedGetUserMessage, result["GetUserMessage.cs"]);
    }


    [Fact]
    public void BuildType_MessageWrapper_Test()
    {
        var lambdaServiceSdkBuilder = new OpenApiSchemaCSharpTypeBuilder(BaseNameSpace);

        var schemaBuilder = new SchemaBuilder();
        schemaBuilder.AddSchema(typeof(MessageWrapper<GetUserMessage>));

        var result = lambdaServiceSdkBuilder.BuildType(schemaBuilder.Build().Last());

        var expectedMessageWrapper = LoadExpected("LambdaService_GetUserMessageMessageWrapper");
        Assert.Equal("GetUserMessageMessageWrapper.cs", result.Name);
        Assert.Equal(expectedMessageWrapper, result.Lines.ToText());
    }

    [Fact]
    public void BuildType_HasDictionary_Test()
    {
        var lambdaServiceSdkBuilder = new OpenApiSchemaCSharpTypeBuilder(BaseNameSpace);

        var schemaBuilder = new SchemaBuilder();
        schemaBuilder.AddSchema(typeof(MessageWrapper<GetUserMessage>));

        var result = lambdaServiceSdkBuilder.BuildType(schemaBuilder.Build().Last());

        var expectedMessageWrapper = LoadExpected("LambdaService_GetUserMessageMessageWrapper");
        Assert.Equal("GetUserMessageMessageWrapper.cs", result.Name);
        Assert.Equal(expectedMessageWrapper, result.Lines.ToText());
    }
}
