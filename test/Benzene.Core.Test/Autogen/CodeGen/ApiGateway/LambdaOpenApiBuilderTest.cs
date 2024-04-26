using System.IO;
using Benzene.Abstractions.MessageHandling;
using Benzene.CodeGen.ApiGateway;
using Benzene.CodeGen.Core;
using Benzene.Core.MessageHandling;
using Benzene.Http;
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
            
        var apiGatewayBuilderV1 = new ApiGatewayBuilderV1("platform_marketplace_core_func_uri");

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


        var apiGatewayBuilderV1 = new ApiGatewayBuilderV1("platform_rbac_bff_func_uri");

        var result = apiGatewayBuilderV1.BuildCodeFiles(eventServiceDocument).ToFilesDictionary();

        Assert.Equal(expected, result["openApi.yaml"], ignoreLineEndingDifferences: true);
    }
}
