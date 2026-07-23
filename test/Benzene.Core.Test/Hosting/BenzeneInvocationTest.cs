using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.AspNet.Core;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Hosting;

public class InvocationInfoResponse
{
    public string InvocationId { get; set; }
    public string Platform { get; set; }
    public bool HasLambdaContext { get; set; }
    public bool HasHttpContext { get; set; }
}

// Not [Message]/[HttpEndpoint]-attributed: see the note on PingHandler in AspNetUnifiedStartUpTest.cs
// for why -- this handler is registered explicitly via DI instead.
public class InvocationInfoHandler : IMessageHandler<Void, InvocationInfoResponse>
{
    private readonly IBenzeneInvocation _invocation;

    public InvocationInfoHandler(IBenzeneInvocation invocation)
    {
        _invocation = invocation;
    }

    public Task<IBenzeneResult<InvocationInfoResponse>> HandleAsync(Void request) =>
        Task.FromResult(BenzeneResult.Ok(new InvocationInfoResponse
        {
            InvocationId = _invocation.InvocationId,
            Platform = _invocation.Platform,
            HasLambdaContext = _invocation.GetFeature<ILambdaContext>() != null,
            HasHttpContext = _invocation.GetFeature<HttpContext>() != null
        }));
}

// Note: IBenzeneInvocation is populated per pipeline scope, same as the existing log-context enrichers
// (WithRequestId() etc.) -- it does not automatically flow into a nested sub-application that creates
// its own DI scope, such as UseBenzeneMessage's per-message dispatch (used for SQS/SNS batches too).
// This test therefore reads IBenzeneInvocation directly from the same AwsEventStreamContext-level
// pipeline UseBenzeneInvocation() was added to, which is the level real apps enrich logs/tracing at.
public class AwsInvocationStartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x.AddBenzene());

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseAwsLambda(aws => aws
            .UseBenzeneInvocation()
            .Use("WriteInvocationInfo", resolver => async (context, next) =>
            {
                var invocation = resolver.GetService<IBenzeneInvocation>();
                var info = new InvocationInfoResponse
                {
                    InvocationId = invocation.InvocationId,
                    Platform = invocation.Platform,
                    HasLambdaContext = invocation.GetFeature<ILambdaContext>() != null,
                    HasHttpContext = invocation.GetFeature<HttpContext>() != null
                };
                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(info));
                await context.Response.WriteAsync(bytes, 0, bytes.Length);
                context.Response.Position = 0;
                await next();
            }));
}

public class AspNetInvocationStartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x
            .AddBenzene()
            .AddScoped<InvocationInfoHandler>()
            .AddSingleton<IMessageHandlerDefinition>(_ => MessageHandlerDefinition.CreateInstance(
                "invocation-info", "", typeof(Void), typeof(InvocationInfoResponse), typeof(InvocationInfoHandler)))
            .AddSingleton<Benzene.Http.Routing.IHttpEndpointDefinition>(_ =>
                new Benzene.Http.Routing.HttpEndpointDefinition("GET", "/invocation-info", "invocation-info")));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseHttp(http => http
            .UseBenzeneInvocation()
            .UseMessageHandlers(Array.Empty<Type>()));
}

public class BenzeneInvocationTest
{
    [Fact]
    public async Task AwsLambda_ExposesLambdaContextFeature_NotHttpContext()
    {
        using var host = new AwsLambdaBenzeneTestHost(new AwsLambdaHost<AwsInvocationStartUp>());

        var responseStream = await host.SendEventAsync(
            new object(),
            new TestLambdaContext { AwsRequestId = "test-request-id" });

        using var reader = new StreamReader(responseStream);
        var info = JsonSerializer.Deserialize<InvocationInfoResponse>(await reader.ReadToEndAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal("test-request-id", info.InvocationId);
        Assert.Equal("AwsLambda", info.Platform);
        Assert.True(info.HasLambdaContext);
        Assert.False(info.HasHttpContext);
    }

    [Fact]
    public async Task AspNet_ExposesHttpContextFeature_NotLambdaContext()
    {
        var builder = WebApplication.CreateBuilder();
        builder.UseBenzene<AspNetInvocationStartUp>();
        var app = builder.Build();
        app.UseBenzene();

        var requestDelegate = ((IApplicationBuilder)app).Build();

        var httpContext = new DefaultHttpContext
        {
            Request = { Method = "GET", Path = "/invocation-info" },
            Response = { Body = new MemoryStream() },
            TraceIdentifier = "test-trace-id"
        };

        await requestDelegate(httpContext);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var info = await JsonSerializer.DeserializeAsync<InvocationInfoResponse>(httpContext.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal("test-trace-id", info.InvocationId);
        Assert.Equal("AspNet", info.Platform);
        Assert.False(info.HasLambdaContext);
        Assert.True(info.HasHttpContext);
    }
}
