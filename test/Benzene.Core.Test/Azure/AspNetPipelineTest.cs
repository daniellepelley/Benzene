using System.Threading.Tasks;
using Benzene.Azure.AspNet;
using Benzene.Azure.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.DataAnnotations;
using Benzene.Results;
using Benzene.Test.Examples;
using Benzene.Tools;
using Benzene.Xml;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Benzene.Test.Azure;

public class AspNetPipelineTest
{
    private static readonly ExampleRequestPayload Payload = new() { Name = "some-message" };

    [Fact]
    public async Task Send()
    {
        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseHttp(http => http
                    .UseMessageHandlers()))
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
        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection()
            ).Configure(app => app
                .UseHttp(http => http
                    .UseXml()
                    .UseMessageHandlers()))
            .Build();

        var request = HttpBuilder.Create("GET", "/example", Payload)
                .WithHeader("content-type", "application/xml")
                .AsAspNetCoreHttpRequest(new XmlSerializer());

        var response = await app.HandleHttpRequest(request) as ContentResult;

        Assert.NotNull(response);
        Assert.NotNull(response.Content);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("application/xml", response.ContentType);
    }

    [Fact]
    public async Task Send_ValidationError()
    {
        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services .ConfigureServiceCollection())
            .Configure(app => app
                .UseHttp(http => http
                    .UseMessageHandlers(x => x.UseDataAnnotationsValidation())))
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
        Assert.NotEmpty(payload.Detail);
    }
}
