using System;
using System.Collections.Generic;
using System.IO;
using Benzene.CodeGen.Core;
using Benzene.CodeGen.Core.Writers;
using Benzene.CodeGen.Markdown;
using Benzene.Test.Autogen.CodeGen.Helpers;
using Benzene.Test.Autogen.CodeGen.Model;
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
}
