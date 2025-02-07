using Amazon.Lambda.APIGatewayEvents;
using Benzene.Aws.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Test.Examples;
using Benzene.Testing;
using Xunit;

namespace Benzene.Test.Aws.ApiGateway;

public class ApiGatewayMessageBodyGetterTest
{
    private static APIGatewayProxyRequest CreateRequest()
    {
        return HttpBuilder.Create("GET", "/example", new { name = "some-message" }).AsApiGatewayRequest();
    }

    [Fact]
    public void Map()
    {
        var mapper = new RequestMapper<ApiGatewayContext>(new ApiGatewayMessageBodyGetter(), new JsonSerializer());
        var request = mapper.GetBody<ExampleRequestPayload>(new ApiGatewayContext(CreateRequest()));

        Assert.Equal("some-message", request.Name);
    }

    // [Fact]
    // public void Map_Xml()
    // {
    //     var mapper = new ApiGatewayMessageBodyGetter(new RouteFinder(new HttpEndpointFinder(GetType().Assembly)));
    //     //var xml = @"<?xml version=""1.0"" encoding=""UTF-8"" ?><rootElement><name>some-message</name><payload><value>some-value</value></payload></rootElement>";
    //     var xml = File.ReadAllText("Examples/xmlPayload.xml");
    //
    //     var apiGatewayProxyRequest = new APIGatewayProxyRequest
    //         {
    //             HttpMethod = "GET",
    //             Path = "/example",
    //             Body = xml,
    //             Headers = new Dictionary<string, string>
    //             {
    //                 {
    //                     "x-correlation-id", Guid.NewGuid().ToString()
    //                 },
    //                 {
    //                     "content-type", "application/xml"
    //                 }
    //             },
    //         };
    //
    //         
    //     var request = mapper.GetBody<Examples.ExamplePayload>(new ApiGatewayContext(apiGatewayProxyRequest));
    //
    //     Assert.Equal("some-message", request.Name);
    //
    // }
}
