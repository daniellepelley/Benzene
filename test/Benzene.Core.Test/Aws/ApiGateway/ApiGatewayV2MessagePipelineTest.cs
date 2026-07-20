using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway.TestHelpers;
using Benzene.Aws.Lambda.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Http;
using Benzene.Http.Routing;
using Benzene.Test.Aws.ApiGateway.Examples;
using Benzene.Test.Examples;
using Benzene.Testing;
using Benzene.Tools.Aws;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.ApiGateway;

/// <summary>
/// Covers the API Gateway HTTP API (payload format version 2.0) router added for #25(A): a v2 proxy
/// event routes through <c>UseApiGatewayV2</c>, v1 and v2 routers coexist in one Lambda and each claims
/// only its own payload shape, and a v2 event declines a v1-only pipeline rather than being mis-handled.
/// </summary>
public class ApiGatewayV2MessagePipelineTest
{
    private static APIGatewayHttpApiV2ProxyRequest CreateV2Request(string method, string path, object body = null)
    {
        return new APIGatewayHttpApiV2ProxyRequest
        {
            Version = "2.0",
            RawPath = path,
            RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
            {
                Http = new APIGatewayHttpApiV2ProxyRequest.HttpDescription
                {
                    Method = method,
                    Path = path
                }
            },
            Headers = new Dictionary<string, string> { { "x-correlation-id", Guid.NewGuid().ToString() } },
            Body = body == null ? null : new JsonSerializer().Serialize(body)
        };
    }

    [Fact]
    public async Task Send_V2Event_RoutesThroughV2Pipeline()
    {
        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseApiGatewayV2(apiGateway => apiGateway
                    .UseMessageHandlers()))
            .BuildHost();

        var response = await host.SendEventAsync<APIGatewayHttpApiV2ProxyResponse>(
            CreateV2Request("GET", Defaults.Path, Defaults.MessageAsObject));

        Assert.NotNull(response);
        Assert.NotNull(response.Body);
        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task V1AndV2_RegisteredTogether_EachRoutesToItsOwnPayloadShape()
    {
        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway.UseMessageHandlers())
                .UseApiGatewayV2(apiGateway => apiGateway.UseMessageHandlers()))
            .BuildHost();

        // A v2 event is claimed by the v2 router even though the v1 router is registered first.
        var v2Response = await host.SendEventAsync<APIGatewayHttpApiV2ProxyResponse>(
            CreateV2Request("GET", Defaults.Path, Defaults.MessageAsObject));
        Assert.Equal(200, v2Response.StatusCode);

        // A v1 event is still claimed by the v1 router.
        var v1Request = HttpBuilder.Create("GET", Defaults.Path, Defaults.MessageAsObject)
            .WithHeader("x-correlation-id", Guid.NewGuid().ToString())
            .AsApiGatewayRequest();
        var v1Response = await host.SendApiGatewayAsync(v1Request);
        Assert.Equal(200, v1Response.StatusCode);
    }

    [Fact]
    public async Task V2Event_WithOnlyV1Registered_IsDeclined()
    {
        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway.UseMessageHandlers()))
            .BuildHost();

        // The v1 router declines a v2-shaped event (no top-level HttpMethod), so nothing writes a
        // response — the empty response stream deserializes to null rather than a handled 200.
        var response = await host.SendEventAsync<APIGatewayHttpApiV2ProxyResponse>(
            CreateV2Request("GET", Defaults.Path, Defaults.MessageAsObject));

        Assert.Null(response);
    }

    [Fact]
    public void Mapper_ResolvesTopicHeadersAndFoldsCookies()
    {
        var mockHttpEndpointFinder = new Mock<IHttpEndpointFinder>();
        mockHttpEndpointFinder.Setup(x => x.FindDefinitions())
            .Returns(new[]
            {
                new HttpEndpointDefinition("GET", "example", Defaults.Topic) as IHttpEndpointDefinition
            });

        var request = CreateV2Request("GET", Defaults.Path, Defaults.MessageAsObject);
        request.Cookies = new[] { "session=abc", "theme=dark" };
        var context = new ApiGatewayV2Context(request);

        var httpHeaderMappings = new HttpHeaderMappings(new Dictionary<string, string> { { "x-correlation-id", "correlationId" } });

        var body = new RequestMapper<ApiGatewayV2Context>(new ApiGatewayV2MessageBodyGetter(), new JsonSerializer()).GetBody<ExampleRequestPayload>(context);
        var topic = new ApiGatewayV2MessageTopicGetter(new RouteFinder(mockHttpEndpointFinder.Object)).GetTopic(context);
        var headers = new ApiGatewayV2MessageHeadersGetter(httpHeaderMappings).GetHeaders(context);

        Assert.Equal(Defaults.Name, body.Name);
        Assert.Equal(Defaults.Topic, topic.Id);
        Assert.True(headers.ContainsKey("correlationId"));
        // The v2 cookies array is folded into a single request "cookie" header.
        Assert.Equal("session=abc; theme=dark", headers["cookie"]);
    }
}
