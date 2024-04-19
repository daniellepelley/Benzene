using Amazon.Lambda.APIGatewayEvents;
using Benzene.Aws.ApiGateway;
using Benzene.Core.Request;
using Benzene.Core.Serialization;
using Benzene.Test.Examples;
using Benzene.Tools;
using Xunit;

namespace Benzene.Test.Aws.ApiGateway;

public class ApiGatewayMessageBodyMapperTest
{
    private static APIGatewayProxyRequest CreateRequest()
    {
        return HttpBuilder.Create("GET", "/example", new { name = "some-message" }).AsApiGatewayRequest();
    }

    [Fact]
    public void Map()
    {
        var mapper = new RequestMapper<ApiGatewayContext>(new ApiGatewayMessageBodyMapper(), new JsonSerializer());
        var request = mapper.GetBody<ExampleRequestPayload>(new ApiGatewayContext(CreateRequest()));

        Assert.Equal("some-message", request.Name);
    }

    // [Fact]
    // public void Map_Xml()
    // {
    //     var mapper = new ApiGatewayMessageBodyMapper(new RouteFinder(new HttpEndpointFinder(GetType().Assembly)));
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
