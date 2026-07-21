using System;
using System.Collections.Generic;
using System.IO;
using Benzene.CodeGen.Core;
using Benzene.CodeGen.Core.Writers;
using Benzene.CodeGen.Markdown;
using Benzene.Schema.OpenApi.Examples;
using Benzene.Test.Autogen.CodeGen.Helpers;
using Benzene.Test.Autogen.CodeGen.Model;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Markdown;

public class MarkdownTypeBuilderTest
{
    private string LoadExpected(string fileName) =>
        File.ReadAllText($"{Directory.GetCurrentDirectory()}/Autogen/CodeGen/Markdown/Examples/{fileName}.md");

    [Fact]
    public void BuildType_GetUserMessage_Test()
    {
        var expected = LoadExpected("GetTenantMessage");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "tenant:get", (typeof(GetTenantMessage), typeof(GetTenantMessage), typeof(TenantDto)) },
            { "tenant:create", (typeof(CreateTenantMessage), typeof(CreateTenantMessage), typeof(TenantDto)) }
        };

        var lineWriter = new LineWriter();
        
        var markdownTypeBuilder = new MarkdownTypeBuilder(new SchemaGetter( dictionary.ToOpenApiSchemas()));

        markdownTypeBuilder.BuildType("GetTenantMessage", lineWriter);

        Assert.Equal(expected, lineWriter.GetLines().ToText(), ignoreLineEndingDifferences: true);
    }
    
    [Fact]
    public void BuildType_TenantDto_Test()
    {
        var expected = LoadExpected("TenantDto");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "tenant:get", (typeof(GetTenantMessage), typeof(GetTenantMessage), typeof(TenantDto)) },
            { "tenant:create", (typeof(CreateTenantMessage), typeof(CreateTenantMessage), typeof(TenantDto)) }
        };

        var lineWriter = new LineWriter();
        
        var markdownTypeBuilder = new MarkdownTypeBuilder(new SchemaGetter( dictionary.ToOpenApiSchemas()));

        markdownTypeBuilder.BuildType("TenantDto", lineWriter);

        Assert.Equal(expected, lineWriter.GetLines().ToText(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BuildType_ArrayOfReferencedObjects_ExpandsItemProperties()
    {
        // A property that is an array of a referenced object (List<TenantDto>) should render each
        // item's fields, exactly like the single-object case does. The buggy `||` in MapProperty
        // collapsed every referenced-object array to "{...}[]" (short-circuiting on Reference != null),
        // so the item's fields were lost from the generated docs.
        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "tenant:list", (typeof(GetTenantMessage), typeof(GetTenantMessage), typeof(TenantListDto)) }
        };

        var lineWriter = new LineWriter();
        var markdownTypeBuilder = new MarkdownTypeBuilder(new SchemaGetter(dictionary.ToOpenApiSchemas()));

        markdownTypeBuilder.BuildType("TenantListDto", lineWriter);

        var expected =
            "{" + Environment.NewLine +
            "    tenants: {" + Environment.NewLine +
            "        id: guid" + Environment.NewLine +
            "        name: string" + Environment.NewLine +
            "        crn: string" + Environment.NewLine +
            "        internal: {" + Environment.NewLine +
            "            value1: string" + Environment.NewLine +
            "            value2: {...}" + Environment.NewLine +
            "        }" + Environment.NewLine +
            "    }[]" + Environment.NewLine +
            "}" + Environment.NewLine;

        Assert.Equal(expected, lineWriter.GetLines().ToText(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BuildType_ArrayOfInlineObjects_DoesNotThrow()
    {
        // An array whose items are an inline object (no $ref) reaches the array branch via
        // Items.Type == "object" with Items.Reference == null. The buggy `||` then evaluated
        // Items.Reference.ReferenceV2 on a null Reference -> NullReferenceException. The item's
        // inline fields should be expanded instead.
        var itemSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                { "name", new OpenApiSchema { Type = "string" } }
            }
        };
        var rootSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                { "items", new OpenApiSchema { Type = "array", Items = itemSchema } }
            }
        };

        var schemas = new Dictionary<string, OpenApiSchema> { { "Root", rootSchema } };
        var lineWriter = new LineWriter();
        var markdownTypeBuilder = new MarkdownTypeBuilder(new SchemaGetter(schemas));

        markdownTypeBuilder.BuildType("Root", lineWriter);

        Assert.Contains("name: string", lineWriter.GetLines().ToText());
    }
}
