using System.Collections.Generic;
using System.IO;
using Benzene.Abstractions.MessageHandlers;
using Benzene.CodeGen.ApiGateway;
using Benzene.CodeGen.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Http.Routing;
using Benzene.Schema.OpenApi.EventService;
using Benzene.Test.Autogen.CodeGen.Model;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.ApiGateway;

public class LambdaOpenApiBuilderTest
{
    private string LoadExpected(string fileName) => File.ReadAllText($"{Directory.GetCurrentDirectory()}/Autogen/CodeGen/ApiGateway/Examples/{fileName}.yaml");
        
    [Fact]
    public void BuildsSdk_UserGet_Test()
    {
        var expected = LoadExpected("GetUser");

        var messageHandlerDefinitions = new IMessageHandlerDefinition []{
            MessageHandlerDefinition.CreateInstance("user:get", typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)),
            MessageHandlerDefinition.CreateInstance("user:update", typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto))
        };

        var httpEndpointDefinitions = new[] {
            HttpEndpointDefinition.CreateInstance("GET", "rbac/user/{id}", "user:get"),
            HttpEndpointDefinition.CreateInstance("PUT", "rbac/user/{id}", "user:update")
        };

        var eventServiceDocument = httpEndpointDefinitions.ToEventServiceDocument(messageHandlerDefinitions);
            
        var apiGatewayBuilderV1 = new ApiGatewayBuilderV1("BENZENE_MARKETPLACE_CORE_FUNC_URI");

        var result = apiGatewayBuilderV1.BuildCodeFiles(eventServiceDocument).ToFilesDictionary();

        Assert.Equal(expected, result["openApi.yaml"], ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BuildsSdk_RbacTest_Test()
    {
        var expected = LoadExpected("RbacTest");

        var messageHandlerDefinitions = new IMessageHandlerDefinition[]{
            MessageHandlerDefinition.CreateInstance("rbac:test", typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)),
        };

        var httpEndpointDefinitions = new[] {
            HttpEndpointDefinition.CreateInstance("GET", "rbac/test", "rbac:test"),
        };

        var eventServiceDocument = httpEndpointDefinitions.ToEventServiceDocument(messageHandlerDefinitions);


        var apiGatewayBuilderV1 = new ApiGatewayBuilderV1("BENZENE_RBAC_BFF_FUNC_URI");

        var result = apiGatewayBuilderV1.BuildCodeFiles(eventServiceDocument).ToFilesDictionary();

        Assert.Equal(expected, result["openApi.yaml"], ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Options_ApplyAuthorizerIdentityHeadersAndExclusions()
    {
        var messageHandlerDefinitions = new IMessageHandlerDefinition[]{
            MessageHandlerDefinition.CreateInstance("user:get", typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)),
            MessageHandlerDefinition.CreateInstance("user:signup", typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)),
        };
        var httpEndpointDefinitions = new[] {
            HttpEndpointDefinition.CreateInstance("GET", "user/{id}", "user:get"),
            HttpEndpointDefinition.CreateInstance("POST", "signup", "user:signup"),
        };
        var eventServiceDocument = httpEndpointDefinitions.ToEventServiceDocument(messageHandlerDefinitions);

        var options = new ApiGatewayOptions("MY_FUNC_URI")
        {
            AuthorizerName = "MyCustomAuthoriser",
            UnauthenticatedTopics = new[] { "user:signup" },
            AllowedHeaders = "Authorization,Content-Type,X-Api-Key,X-Tenant-Id",
            IdentityHeaders = new Dictionary<string, string> { ["x-user-id"] = "$context.authorizer.userid" },
        };

        var result = new ApiGatewayBuilderV1(options).BuildCodeFiles(eventServiceDocument).ToFilesDictionary();
        var yaml = result["openApi.yaml"];

        // Secured topic carries the configured authorizer; the excluded (public) topic does not.
        Assert.Contains("- MyCustomAuthoriser: []", yaml);
        Assert.Contains("- api_key: []", yaml);
        // The excluded topic's operation has api_key but no custom authorizer line above it.
        Assert.DoesNotContain("PlatformTenantId", yaml);
        // Configured identity header and allow-headers are injected; no hard-coded company values remain.
        Assert.Contains("\"x-user-id\":\"$context.authorizer.userid\"", yaml);
        Assert.Contains("'Authorization,Content-Type,X-Api-Key,X-Tenant-Id'", yaml);
    }

    [Fact]
    public void Default_HasNoCustomAuthorizer_AndNoInjectedIdentityHeaders()
    {
        var messageHandlerDefinitions = new IMessageHandlerDefinition[]{
            MessageHandlerDefinition.CreateInstance("user:get", typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)),
        };
        var httpEndpointDefinitions = new[] {
            HttpEndpointDefinition.CreateInstance("GET", "user/{id}", "user:get"),
        };
        var eventServiceDocument = httpEndpointDefinitions.ToEventServiceDocument(messageHandlerDefinitions);

        var yaml = new ApiGatewayBuilderV1("MY_FUNC_URI").BuildCodeFiles(eventServiceDocument).ToFilesDictionary()["openApi.yaml"];

        // The generic default is company-free: api_key only, and only transport identity headers.
        Assert.Contains("- api_key: []", yaml);
        Assert.DoesNotContain("Authoriser", yaml);
        Assert.DoesNotContain("$context.authorizer", yaml);
        Assert.Contains("\"UserAgent\": \"$context.identity.userAgent\"", yaml);
    }
}
