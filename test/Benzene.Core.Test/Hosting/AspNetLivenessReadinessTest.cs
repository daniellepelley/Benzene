using System.IO;
using System.Text;
using System.Threading.Tasks;
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
using Moq;
using Xunit;

namespace Benzene.Test.Hosting;

// Verifies the ASP.NET Core wiring pattern documented in docs/kubernetes-health-checks.md: since
// UseLivenessCheck/UseReadinessCheck are topic-based (Benzene.HealthChecks.Extensions), reaching them
// over ASP.NET Core's raw HTTP pipeline requires registering an IHttpEndpointDefinition mapping the
// conventional /livez, /readyz paths to Constants.DefaultLivenessTopic/DefaultReadinessTopic - no
// IMessageHandlerDefinition is needed, since this is raw middleware intercepting before
// .UseMessageHandlers() would otherwise resolve a handler.
public class AspNetLivenessAndReadinessStartUp : BenzeneStartUp
{
    public static Mock<IHealthCheck> LivenessCheck { get; private set; }
    public static Mock<IHealthCheck> ReadinessCheck { get; private set; }

    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        LivenessCheck = new Mock<IHealthCheck>();
        LivenessCheck.Setup(x => x.Type).Returns("Liveness");
        LivenessCheck.Setup(x => x.ExecuteAsync()).ReturnsAsync(HealthCheckResult.CreateInstance(true, "Liveness"));

        ReadinessCheck = new Mock<IHealthCheck>();
        ReadinessCheck.Setup(x => x.Type).Returns("Readiness");
        ReadinessCheck.Setup(x => x.ExecuteAsync()).ReturnsAsync(HealthCheckResult.CreateInstance(false, "Readiness"));

        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers()
            .AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("GET", "/livez", Constants.DefaultLivenessTopic))
            .AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("GET", "/readyz", Constants.DefaultReadinessTopic)));
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseHttp(http => http
            .UseLivenessCheck(LivenessCheck.Object)
            .UseReadinessCheck(ReadinessCheck.Object));
}

public class AspNetLivenessReadinessTest
{
    // AspApplicationBuilder falls through to next() unless Response.HasStarted, which DefaultHttpContext
    // never flips to true for a bare in-memory response body (no real transport to flush) -- so the
    // response body, not the status code, is the reliable signal in this test harness that Benzene
    // actually handled the request (same caveat documented in AspNetUnifiedStartUpTest.cs).
    private static async Task<string> SendAsync(string method, string path)
    {
        var builder = WebApplication.CreateBuilder();
        builder.UseBenzene<AspNetLivenessAndReadinessStartUp>();

        var app = builder.Build();
        app.UseBenzene();

        var requestDelegate = ((IApplicationBuilder)app).Build();

        var httpContext = new DefaultHttpContext
        {
            Request = { Method = method, Path = path },
            Response = { Body = new MemoryStream() }
        };

        await requestDelegate(httpContext);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(httpContext.Response.Body, Encoding.UTF8).ReadToEndAsync();
    }

    [Fact]
    public async Task LivenessEndpoint_RunsOnlyLivenessCheck()
    {
        var body = await SendAsync("GET", "/livez");

        AspNetLivenessAndReadinessStartUp.LivenessCheck.Verify(x => x.ExecuteAsync(), Times.Once);
        AspNetLivenessAndReadinessStartUp.ReadinessCheck.Verify(x => x.ExecuteAsync(), Times.Never);
        Assert.Contains("Liveness", body);
        Assert.Contains("\"isHealthy\":true", body);
    }

    [Fact]
    public async Task ReadinessEndpoint_UnhealthyCheck_ReportsUnhealthyInBody()
    {
        var body = await SendAsync("GET", "/readyz");

        AspNetLivenessAndReadinessStartUp.ReadinessCheck.Verify(x => x.ExecuteAsync(), Times.Once);
        AspNetLivenessAndReadinessStartUp.LivenessCheck.Verify(x => x.ExecuteAsync(), Times.Never);
        Assert.Contains("Readiness", body);
        Assert.Contains("\"isHealthy\":false", body);
    }
}
