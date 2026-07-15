using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Benzene.Abstractions.Hosting;
using Benzene.GoogleCloud.Functions.Http;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Hosting;

// A StartUp that never calls app.UseHttp(...), to exercise GoogleCloudFunctionApplicationBuilder's
// guard clause.
public class GoogleCloudNoHttpStartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) { }
}

public class GoogleCloudFunctionHostTest
{
    [Fact]
    public async Task HandleAsync_RunsTheSameSharedStartUpThatWorksOnAspNetCore()
    {
        // AspNetSharedStartUp/PingHandler (Hosting/AspNetUnifiedStartUpTest.cs) are the same
        // BenzeneStartUp already proven to work unchanged over a real ASP.NET Core host - reusing
        // it here directly proves GoogleCloudFunctionApplicationBuilder's own design claim (see its
        // remarks): the same StartUp works unchanged on Google Cloud Functions too.
        var host = new GoogleCloudFunctionHost<AspNetSharedStartUp>();

        var requestBody = Encoding.UTF8.GetBytes("{\"name\":\"world\"}");
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Method = "POST",
                Path = "/ping",
                ContentType = "application/json",
                Body = new MemoryStream(requestBody)
            },
            Response = { Body = new MemoryStream() }
        };

        await host.HandleAsync(httpContext);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(httpContext.Response.Body, Encoding.UTF8).ReadToEndAsync();
        Assert.Contains("pong-world", body);
    }

    [Fact]
    public void Constructor_StartUpNeverCallsUseHttp_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new GoogleCloudFunctionHost<GoogleCloudNoHttpStartUp>());

        Assert.Contains("UseHttp", exception.Message);
    }
}
