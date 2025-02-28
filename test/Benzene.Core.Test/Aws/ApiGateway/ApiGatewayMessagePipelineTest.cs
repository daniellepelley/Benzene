﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Autofac;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Autofac;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Middleware;
using Benzene.FluentValidation;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Http.Routing;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.ApiGateway.Examples;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Benzene.Testing;
using Benzene.Tools.Aws;
using Benzene.Xml;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Aws.ApiGateway;

public class ApiGatewayMessagePipelineTest
{
    private static APIGatewayProxyRequest CreateRequest()
    {
        return HttpBuilder.Create("GET", Defaults.Path, Defaults.MessageAsObject)
            .WithHeader("x-correlation-id", Guid.NewGuid().ToString())
            .AsApiGatewayRequest();
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
    public async Task SendNullBody()
    {
        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
            )
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseMessageHandlers(x => x.UseFluentValidation())
                )
            ).BuildHost();

        var request = HttpBuilder.Create("GET", Defaults.PathWithParam.Replace("{id}", Defaults.Id.ToString()))
                    .WithHeader("x-correlation-id", Guid.NewGuid().ToString())
                    .AsApiGatewayRequest();
        var response = await host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Body);
        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task SendNullBody_TypeError()
    {
        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
            )
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseMessageHandlers(x => x.UseFluentValidation())
                )
            ).BuildHost();

        var request = HttpBuilder.Create("GET", Defaults.PathWithParam.Replace("{id}", "wrong-type"))
                    .WithHeader("x-correlation-id", Guid.NewGuid().ToString())
                    .AsApiGatewayRequest();
        var response = await host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Body);
        Assert.Equal(400, response.StatusCode);
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
                    .UseMessageHandlers()
                )
            ).BuildHost();

        var request = HttpBuilder.Create("GET", "/example", new ExampleRequestPayload())
            .WithHeader("content-type", "application/xml")
            .AsApiGatewayRequest(new XmlSerializer());

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
        var actualMessage = new RequestMapper<ApiGatewayContext>(new ApiGatewayMessageBodyGetter(), new JsonSerializer()).GetBody<ExampleRequestPayload>(apiGatewayContext);
        var actualTopic = new ApiGatewayMessageTopicGetter(new RouteFinder(mockHttpEndpointFinder.Object)).GetTopic(apiGatewayContext);
        var headers = new ApiGatewayMessageHeadersGetter(httpHeaderMappings).GetHeaders(apiGatewayContext);
        var correlationId = new ApiGatewayMessageHeadersGetter(httpHeaderMappings).GetHeader(apiGatewayContext, "correlationId");

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
                services.RegisterType<BenzeneMessageGetter>();
                services.RegisterInstance(Mock.Of<IExampleService>());

            })
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
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
