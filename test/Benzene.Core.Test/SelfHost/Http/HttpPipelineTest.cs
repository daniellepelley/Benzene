using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.Response;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Core.Response;
using Benzene.DataAnnotations;
using Benzene.HealthChecks.Core;
using Benzene.Results;
using Benzene.SelfHost;
using Benzene.SelfHost.Http;
using Benzene.Test.Examples;
using Benzene.Tools;
using Benzene.Xml;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Benzene.Test.SelfHost.Http;

public class HttpPipelineTest
{
    private static string CreateRequest()
    {
        return HttpBuilder.Create("GET", "/example", Defaults.MessageAsObject).AsRawHttpRequest();
    }

    [Fact]
    public async Task Send()
    {
        var app = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
            )
            .Configure(app => app
                .UseHttp(apiGateway => apiGateway
                    .UseProcessResponse()
                    .UseMessageRouter()

                )
            ).Build();

        var request = CreateRequest();
        var response = await app.HandleAsync(request);

        Assert.NotNull(response);

        var httpResponse = HttpResponseParser.Parse(response);

        Assert.NotNull(httpResponse.Body);
        Assert.Equal(200, httpResponse.StatusCode);
    }

    [Fact]
    public async Task Send_Xml()
    {
        var app = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
            )
            .Configure(app => app
                .UseHttp(apiGateway => apiGateway
                    .UseXml()
                    .UseProcessResponse()
                    .UseMessageRouter())
            ).Build();
    
        var request = HttpBuilder.Create("GET", "/example", Defaults.MessageAsObject)
            .WithHeader("content-type", "application/xml")
            .AsRawHttpRequest();
    
        var response = await app.HandleAsync(request);
    
        Assert.NotNull(response);
        
        var httpResponse = HttpResponseParser.Parse(response);
        
        Assert.NotNull(httpResponse.Body);
        Assert.Equal(200, httpResponse.StatusCode);
    }
    
    [Fact]
    public async Task Send_ValidationError()
    {
        var app = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
            )
            .Configure(app => app
                .UseHttp(apiGateway => apiGateway
                    .UseProcessResponse()
                    .UseMessageRouter(x => x.UseDataAnnotationsValidation())
                )
            ).Build();

        var request = HttpBuilder.Create("GET", "/example", new ExampleRequestPayload
        {
            Name = "12345678901"
        }).AsRawHttpRequest();

        var response = await app.HandleAsync(request);

        Assert.NotNull(response);

        var httpResponse = HttpResponseParser.Parse(response);

        var payload = JsonConvert.DeserializeObject<ErrorPayload>(httpResponse.Body);
        
        Assert.Equal(422, httpResponse.StatusCode);
        Assert.Equal("ValidationError", payload.Status);
        Assert.Single(payload.Errors);
        Assert.NotEmpty(payload.Errors.First());
    }

    [Fact]
    public async Task Send_HealthCheck()
    {
        var mockHealthCheck = new Mock<IHealthCheck>();
        mockHealthCheck.Setup(x => x.ExecuteAsync()).ReturnsAsync(HealthCheckResult.CreateInstance(true,
            null));
    
        var app = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
            )
            .Configure(app => app
                .UseHttp(apiGateway => apiGateway
                    .UseProcessResponse()
                    .UseHealthCheck("healthcheck", "GET", "/healthcheck", mockHealthCheck.Object)
                    .UseMessageRouter()
                )
            ).Build();

        var request = HttpBuilder.Create("GET", "/healthcheck").AsRawHttpRequest();
    
        var response = await app.HandleAsync(request);
    
        Assert.NotNull(response);
    }
}
