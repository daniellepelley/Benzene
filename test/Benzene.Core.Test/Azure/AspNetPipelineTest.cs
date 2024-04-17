using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.Response;
using Benzene.Azure.Core;
using Benzene.Azure.Core.AspNet;
using Benzene.Core.DI;
using Benzene.Core.MessageHandling;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Core.Response;
using Benzene.Core.Serialization;
using Benzene.DataAnnotations;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.SelfHost;
using Benzene.Test.Examples;
using Benzene.Tools;
using Benzene.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;
using Utils = Benzene.Tools.Utils;

namespace Benzene.Test.Azure;

public class AspNetPipelineTest
{
    private static readonly ExampleRequestPayload Payload = new() { Name = "some-message" };

    [Fact]
    public async Task Send()
    {
        var app = new InlineAzureStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseHttp(http => http
                    .UseProcessResponse()
                    .UseMessageRouter()))
            .Build();

        var request = HttpBuilder.Create("GET", "/example", Payload).AsAspNetCoreHttpRequest();

        var response = await app.HandleHttpRequest(request) as ContentResult;
        Assert.NotNull(response);
        Assert.NotNull(response.Content);
        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task Send_Xml()
    {
        var app = new InlineAzureStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection()
            ).Configure(app => app
                .UseHttp(http => http
                    .UseXml()
                    .UseProcessResponse()
                    .UseMessageRouter()))
            .Build();

        var request = HttpBuilder.Create("GET", "/example", Payload)
                .WithHeader("content-type", "application/xml")
                // .WithSerializer(new XmlSerializer())
                .AsAspNetCoreHttpRequest();

        var response = await app.HandleHttpRequest(request) as ContentResult;

        Assert.NotNull(response);
        Assert.NotNull(response.Content);
        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task Send_ValidationError()
    {
        var app = new InlineAzureStartUp()
            .ConfigureServices(services => services .ConfigureServiceCollection())
            .Configure(app => app
                .UseHttp(http => http
                    .UseProcessResponse()
                    .UseMessageRouter(x => x.UseDataAnnotationsValidation())))
            .Build();

        var request = HttpBuilder.Create("GET", "/example", new ExampleRequestPayload
        {
            Name = "12345678901"
        }).AsAspNetCoreHttpRequest();

        var response = await app.HandleHttpRequest(request) as ContentResult;

        Assert.NotNull(response);

        var payload = new JsonSerializer().Deserialize<ErrorPayload>(response.Content);

        Assert.Equal(422, response.StatusCode);
        Assert.Equal("ValidationError", payload.Status);
        Assert.Single(payload.Errors);
        Assert.NotEmpty(payload.Errors.First());
    }
}
