using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers.DI;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Http.Routing;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Hosting;

// A health check that reports the ambient cancellation token's state, to prove the ASP.NET transport
// seeds ICancellationTokenAccessor from HttpContext.RequestAborted.
public class TokenObservingHealthCheck : IHealthCheck
{
    private readonly ICancellationTokenAccessor _accessor;
    public TokenObservingHealthCheck(ICancellationTokenAccessor accessor) => _accessor = accessor;
    public string Type => "TokenObserver";
    public Task<IHealthCheckResult> ExecuteAsync() =>
        Task.FromResult(HealthCheckResult.CreateInstance(true, Type, new Dictionary<string, object>
        {
            { "Cancelled", _accessor.CancellationToken.IsCancellationRequested }
        }));
}

public class AspNetCancellationSeedingStartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers()
            .AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("GET", "/health", Constants.DefaultHealthCheckTopic)));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseHttp(http => http
            .UseHealthCheck("healthcheck", x => x.AddHealthCheck(resolver =>
                new TokenObservingHealthCheck(resolver.GetService<ICancellationTokenAccessor>()))));
}

public class AspNetCancellationSeedingTest
{
    private static async Task<string> SendAsync(CancellationToken requestAborted)
    {
        var builder = WebApplication.CreateBuilder();
        builder.UseBenzene<AspNetCancellationSeedingStartUp>();
        var app = builder.Build();
        app.UseBenzene();
        var requestDelegate = ((IApplicationBuilder)app).Build();

        var httpContext = new DefaultHttpContext
        {
            Request = { Method = "GET", Path = "/health" },
            Response = { Body = new MemoryStream() },
            RequestAborted = requestAborted,
        };

        await requestDelegate(httpContext);
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(httpContext.Response.Body, Encoding.UTF8).ReadToEndAsync();
    }

    [Fact]
    public async Task RequestNotAborted_CheckObservesAnUncancelledToken()
    {
        var body = await SendAsync(CancellationToken.None);

        Assert.Contains("\"Cancelled\":false", body);
    }

    [Fact]
    public async Task RequestAborted_SeedsTheTokenSoTheCheckObservesCancellation()
    {
        var body = await SendAsync(new CancellationToken(canceled: true));

        Assert.Contains("\"Cancelled\":true", body);
    }
}
