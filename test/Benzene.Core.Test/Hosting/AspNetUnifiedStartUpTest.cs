using System.IO;
using System.Text;
using System.Threading.Tasks;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Http.Routing;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Hosting;

public class PingRequest
{
    public string Name { get; set; }
}

public class PingResponse
{
    public string Message { get; set; }
}

// Deliberately not annotated with [Message]/[HttpEndpoint]: those attributes are picked up by ANY
// AppDomain-wide reflection scan (e.g. bare .UseMessageHandlers()), including in unrelated tests
// elsewhere in this assembly, which would inflate their handler/schema counts. Registering the
// definitions explicitly (the same pattern examples/Asp uses for its own "/spec" endpoint) keeps
// this handler visible only within this test's own DI container.
public class PingHandler : IMessageHandler<PingRequest, PingResponse>
{
    public Task<IBenzeneResult<PingResponse>> HandleAsync(PingRequest request)
        => Task.FromResult(BenzeneResult.Ok(new PingResponse { Message = $"pong-{request.Name}" }));
}

public class AspNetSharedStartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers()
            .AddScoped<PingHandler>()
            .AddSingleton<IMessageHandlerDefinition>(_ => MessageHandlerDefinition.CreateInstance(
                "aspnet-unified-ping", "", typeof(PingRequest), typeof(PingResponse), typeof(PingHandler)))
            .AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("POST", "/ping", "aspnet-unified-ping")));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseHttp(http => http.UseMessageHandlers());
}

public class AspNetUnifiedStartUpTest
{
    [Fact]
    public void AspApplicationBuilder_PlatformIdentifier()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.UsingBenzene();
        var app = builder.Build();
        var aspApplicationBuilder = new AspApplicationBuilder(app);

        Assert.Equal("AspNet", aspApplicationBuilder.Platform);
    }

    [Fact]
    public void UseHttp_NoOpOnOtherPlatforms()
    {
        var worker = new Benzene.SelfHost.WorkerApplicationBuilder(new Benzene.Core.Middleware.NullBenzeneServiceContainer());

        var result = worker.UseHttp(_ => { });

        Assert.Same(worker, result);
        Assert.Equal("Worker", result.Platform);
    }

    [Fact]
    public async Task WebApplicationBuilder_UseBenzene_RunsSharedStartUpOverHttp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.UseBenzene<AspNetSharedStartUp>();

        var app = builder.Build();
        app.UseBenzene();

        var requestDelegate = ((IApplicationBuilder)app).Build();

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

        await requestDelegate(httpContext);

        // AspApplicationBuilder falls through to next() unless Response.HasStarted, which DefaultHttpContext
        // never flips to true for a bare in-memory response body (no real transport to flush) -- so the
        // response body, not the final status code, is the reliable signal that Benzene actually handled this.
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(httpContext.Response.Body, Encoding.UTF8).ReadToEndAsync();
        Assert.Contains("pong-world", body);
    }
}
