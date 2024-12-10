using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Autofac;
using Benzene.Abstractions.Request;
using Benzene.Autofac;
using Benzene.Aws.ApiGateway;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.DI;
using Benzene.Core.Mappers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandling;
using Benzene.Core.Middleware;
using Benzene.Core.Request;
using Benzene.Core.Serialization;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Http.Routing;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Aws.ApiGateway.Examples;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Benzene.Tools;
using Benzene.Tools.Aws;
using Benzene.Xml;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.ApiGateway;

public class ApiGatewayMessagePipelineTest
{
    private static APIGatewayProxyRequest CreateRequest()
    {
        return HttpBuilder.Create("GET", "/example", Defaults.MessageAsObject).AsApiGatewayRequest();
    }

    [Fact]
    public async Task Send()
    {
        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
            )
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseProcessResponse()
                    .UseMessageHandlers()
                )
            ).BuildHost();

        var request = CreateRequest();
        var response = await host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Body);
        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task Send_Xml()
    {
        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
            )
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseXml()
                    .UseProcessResponse()
                    .UseMessageHandlers()
                )
            ).BuildHost();

        var request = HttpBuilder.Create("GET", "/example", Defaults.MessageAsObject)
            .WithHeader("content-type", "application/xml")
            .AsApiGatewayRequest();

        var response = await host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        var xml = new XmlSerializer().Deserialize<Void>(response.Body);

        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(xml);
    }

    [Fact]
    public async Task Send_FromStream()
    {
        ApiGatewayContext apiGatewayContext = null;
        var app = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        app.UseApiGateway(message => message
            .Use(null, (context, next) =>
            {
                apiGatewayContext = context;
                context.ApiGatewayProxyResponse = new APIGatewayProxyResponse
                {
                    Body = context.ApiGatewayProxyRequest.Body,
                    StatusCode = 200
                };
                return next();
            })
        );

        var request = CreateRequest();

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(request), ServiceResolverMother.CreateServiceResolver());

        Assert.Equal(Defaults.Message, apiGatewayContext.ApiGatewayProxyResponse.Body);
        Assert.Equal(200, apiGatewayContext.ApiGatewayProxyResponse.StatusCode);
    }


    [Fact]
    public void Mapper()
    {
        var mockHttpEndpointFinder = new Mock<IHttpEndpointFinder>();
        mockHttpEndpointFinder.Setup(x => x.FindDefinitions())
            .Returns(new[]
            {
                new HttpEndpointDefinition("GET", "example", Defaults.Topic) as IHttpEndpointDefinition
            });

        var apiGatewayContext = new ApiGatewayContext(CreateRequest());

        var httpHeaderMappings = new HttpHeaderMappings(new Dictionary<string, string> { { "x-correlation-id", "correlationId" } });
        var actualMessage = new RequestMapper<ApiGatewayContext>(new ApiGatewayMessageBodyMapper(), new JsonSerializer()).GetBody<ExampleRequestPayload>(apiGatewayContext);
        var actualTopic = new ApiGatewayMessageTopicMapper(new RouteFinder(mockHttpEndpointFinder.Object)).GetTopic(apiGatewayContext);
        var headers = new ApiGatewayMessageHeadersMapper(httpHeaderMappings).GetHeaders(apiGatewayContext);
        var correlationId = new ApiGatewayMessageHeadersMapper(httpHeaderMappings).GetHeader(apiGatewayContext, "correlationId");

        Assert.Equal(Defaults.Name, actualMessage.Name);
        Assert.Equal(Defaults.Topic, actualTopic.Id);
        Assert.Single(headers);
        Assert.True(headers.ContainsKey("correlationId"));
        Assert.NotNull(correlationId);
    }

    [Fact]
    public async Task Send_HealthCheck()
    {
        var mockHealthCheck = new Mock<IHealthCheck>();
        mockHealthCheck.Setup(x => x.ExecuteAsync()).ReturnsAsync(HealthCheckResult.CreateInstance(true,
            null));

        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
            )
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseProcessResponse()
                    .UseHealthCheck("healthcheck", "GET", "/healthcheck", mockHealthCheck.Object)
                    .UseMessageHandlers()
                )
            ).BuildHost();

        var request = HttpBuilder.Create("GET", "/healthcheck").AsApiGatewayRequest();
        var response = await host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
    }

    [Fact]
    public async Task Send_WithEnricher()
    {
        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddScoped<IRequestEnricher<ApiGatewayContext>, IpAddressApiGatewayEnricher>()
            )
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseProcessResponse()
                    .UseMessageHandlers()
                )
            ).BuildHost();

        var request = CreateRequest();
        request.RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
        {
            Identity = new APIGatewayProxyRequest.RequestIdentity { SourceIp = "some-ip" }
        };
        var response = await host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Body);
        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task Autofac_Send()
    {
        var assembly = typeof(ExampleRequestPayload).Assembly;
        var host = new AutofacInlineAwsLambdaStartUp()
            .ConfigureServices(services =>
            {
                services
                    .UsingBenzene(x =>
                    {
                        x.AddBenzene();
                        x.AddMessageHandlers(assembly);
                    });
                    services.RegisterType<BenzeneMessageMapper>();
                    services.RegisterInstance(Mock.Of<IExampleService>());

            })
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseProcessResponse()
                    .UseMessageHandlers()
                )
            ).BuildHost();

        var request = CreateRequest();
        var response = await host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Body);
        Assert.Equal(200, response.StatusCode);
    }

}
