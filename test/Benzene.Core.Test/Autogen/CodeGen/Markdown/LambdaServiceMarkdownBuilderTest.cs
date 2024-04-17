using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Benzene.CodeGen.Markdown;
using Benzene.FluentValidation.Schema;
using Benzene.Schema.OpenApi;
using Benzene.Test.Autogen.CodeGen.Client;
using Benzene.Test.Autogen.CodeGen.Helpers;
using Benzene.Test.Autogen.CodeGen.Markdown.Validation;
using Benzene.Test.Autogen.CodeGen.Model;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Markdown;

public class LambdaServiceMarkdownBuilderTest
{
    private const string BaseNameSpace = "This is the header";

    private const string UserLambdaName = "platform-user-core-func";
    private const string UserServiceName = "User";
        
    private const string TenantLambdaName = "platform-tenant-core-func";
    private const string TenantServiceName = "Tenant";

    private string LoadExpected(string fileName) => File.ReadAllText($"{Directory.GetCurrentDirectory()}/Autogen/CodeGen/Markdown/Examples/{fileName}.md");
        
    [Fact]
    public void BuildsSdk_UserGet_Test()
    {
        var expected =LoadExpected("User_Get");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "user:get", (typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)) }
        };

        var fluentValidationSchemaBuilder = new FluentValidationSchemaBuilder(Assembly.GetExecutingAssembly());
        var lambdaServiceSdkBuilder = new LambdaServiceMarkdownBuilder(UserLambdaName, UserServiceName, BaseNameSpace);

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToEventServiceDocument(new OpenApiValidationSchemaBuilder(new SchemaBuilder(), fluentValidationSchemaBuilder)));

        Assert.Equal(expected, result["README.md"], ignoreLineEndingDifferences: true);
    }
        
    [Fact]
    public void BuildsSdk_UserGetAll_Test()
    {
        var expected =LoadExpected("User_GetAll");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "user:get", (typeof(GetAllUserMessage), typeof(GetAllUserMessage), typeof(UserDto[])) }
        };

        var fluentValidationSchemaBuilder = new FluentValidationSchemaBuilder(Assembly.GetExecutingAssembly());
        var lambdaServiceSdkBuilder = new LambdaServiceMarkdownBuilder(UserLambdaName, UserServiceName, BaseNameSpace); 

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToEventServiceDocument(new OpenApiValidationSchemaBuilder(new SchemaBuilder(), fluentValidationSchemaBuilder)));

        Assert.Equal(expected, result["README.md"], ignoreLineEndingDifferences: true);
    }
        
    [Fact]
    public void BuildsSdk_TenantGet_Test()
    {
        var expected = LoadExpected("Tenant_Get");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "tenant:get", (typeof(GetTenantMessage), typeof(GetTenantMessage), typeof(TenantDto)) }
        };

        var fluentValidationSchemaBuilder = new FluentValidationSchemaBuilder(Assembly.GetExecutingAssembly());
        var lambdaServiceSdkBuilder = new LambdaServiceMarkdownBuilder(TenantLambdaName, TenantServiceName, BaseNameSpace); 

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToEventServiceDocument(new OpenApiValidationSchemaBuilder(new SchemaBuilder(), fluentValidationSchemaBuilder)));

        Assert.Equal(expected, result["README.md"], ignoreLineEndingDifferences: true);
    }
        
    [Fact]
    public void BuildsSdk_TenantFull_Test()
    {
        var expected = LoadExpected("Tenant_Full");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "tenant:get", (typeof(GetTenantMessage), typeof(GetTenantMessage), typeof(TenantDto)) },
            { "tenant:create", (typeof(CreateTenantMessage), typeof(CreateTenantMessage), typeof(TenantDto)) }
        };

        var fluentValidationSchemaBuilder = new FluentValidationSchemaBuilder(Assembly.GetExecutingAssembly());
        var lambdaServiceSdkBuilder = new LambdaServiceMarkdownBuilder(TenantLambdaName, TenantServiceName, BaseNameSpace); 

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToEventServiceDocument(new OpenApiValidationSchemaBuilder(new SchemaBuilder(), fluentValidationSchemaBuilder)));

        Assert.Equal(expected, result["README.md"], ignoreLineEndingDifferences: true);
    }
}
