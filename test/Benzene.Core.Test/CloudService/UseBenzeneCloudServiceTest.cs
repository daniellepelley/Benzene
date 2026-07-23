using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway.TestHelpers;
using Benzene.Aws.Lambda.Core;
using Benzene.CloudService;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Test.Examples;
using Benzene.Testing;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Xunit;

namespace Benzene.Test.CloudService;

public class UseBenzeneCloudServiceTest
{
    private static AwsLambdaBenzeneTestHost CreateHost(Action<ICloudServiceBuilder>? configure = null)
    {
        return new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseBenzeneCloudService("orders", configure)
                )
            )
            .BuildHost();
    }

    private static object CreateEnvelope(string topic, string body = Defaults.Message)
    {
        return new
        {
            topic,
            headers = new Dictionary<string, string>(),
            body
        };
    }

    [Fact]
    public async Task EnvelopeEndpoint_IsAtTheDefaultStandardPath()
    {
        var host = CreateHost();

        var response = await host.SendApiGatewayAsync(
            HttpBuilder.Create("POST", CloudServicePaths.Invoke, CreateEnvelope(Defaults.Topic)));

        Assert.Equal(200, response.StatusCode);
        Assert.Contains("\"statusCode\":\"ok\"", response.Body);
    }

    [Fact]
    public async Task HealthEndpoint_IsAtTheDefaultStandardPath()
    {
        var host = CreateHost();

        var response = await host.SendApiGatewayAsync(
            HttpBuilder.Create("GET", CloudServicePaths.Health, new { }));

        Assert.Equal(200, response.StatusCode);
        Assert.Contains("isHealthy", response.Body);
    }

    [Fact]
    public async Task SpecEndpoint_IsAtTheDefaultStandardPath()
    {
        var host = CreateHost();

        var response = await host.SendApiGatewayAsync(
            HttpBuilder.Create("GET", CloudServicePaths.Spec, new { }));

        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheckTopic_IsInterceptedOnTheEnvelopePipeline()
    {
        var host = CreateHost();

        var response = await host.SendApiGatewayAsync(
            HttpBuilder.Create("POST", CloudServicePaths.Invoke, CreateEnvelope("healthcheck", "{}")));

        Assert.Equal(200, response.StatusCode);
        Assert.Contains("isHealthy", response.Body);
    }

    [Fact]
    public async Task MeshTopic_ServesTheDescriptor_WithTheProfileSelfAssessment()
    {
        var host = CreateHost();

        var response = await host.SendApiGatewayAsync(
            HttpBuilder.Create("POST", CloudServicePaths.Invoke, CreateEnvelope("mesh", "{}")));

        Assert.Equal(200, response.StatusCode);
        Assert.Contains("orders", response.Body);
        Assert.Contains(CloudServiceProfileReport.ProfileName, response.Body);
        // No collector configured: the outbound feeds have no destination, so the service honestly
        // reports R6 as missing rather than claiming full conformance.
        Assert.Contains("R6", response.Body);
    }

    [Fact]
    public async Task WithCollector_TheProfileSelfAssessmentIsFullyConformant()
    {
        // Port 9 is the discard service - registration will fail and be retried, which is exactly
        // the spec's degradation behavior: an unreachable collector never affects the service.
        var host = CreateHost(cloud => cloud.WithCollector("http://localhost:9/benzene/invoke"));

        var response = await host.SendApiGatewayAsync(
            HttpBuilder.Create("POST", CloudServicePaths.Invoke, CreateEnvelope("mesh", "{}")));

        Assert.Equal(200, response.StatusCode);
        Assert.Contains(CloudServiceProfileReport.ProfileName, response.Body);
        Assert.DoesNotContain("R6", response.Body);
        Assert.DoesNotContain("R7", response.Body);
        Assert.DoesNotContain("R8", response.Body);
    }

    [Fact]
    public async Task WithoutMesh_TheMeshTopicIsNotIntercepted()
    {
        var host = CreateHost(cloud => cloud.WithoutMesh());

        var response = await host.SendApiGatewayAsync(
            HttpBuilder.Create("POST", CloudServicePaths.Invoke, CreateEnvelope("mesh", "{}")));

        Assert.NotEqual(200, response.StatusCode);
    }

    [Fact]
    public async Task RelocatedSurface_StillWorks_AndIsFlaggedInTheProfileSelfAssessment()
    {
        var host = CreateHost(cloud => cloud.WithInvokePath("/custom-invoke"));

        var response = await host.SendApiGatewayAsync(
            HttpBuilder.Create("POST", "/custom-invoke", CreateEnvelope("mesh", "{}")));

        Assert.Equal(200, response.StatusCode);
        Assert.Contains("R7", response.Body);
    }

    [Fact]
    public async Task DomainTopics_AreRoutedOnBothSurfaces()
    {
        var host = CreateHost();

        var viaEnvelope = await host.SendApiGatewayAsync(
            HttpBuilder.Create("POST", CloudServicePaths.Invoke, CreateEnvelope(Defaults.Topic)));
        var viaHttp = await host.SendApiGatewayAsync(
            HttpBuilder.Create("GET", Defaults.Path, Defaults.MessageAsObject));

        Assert.Equal(200, viaEnvelope.StatusCode);
        Assert.Equal(200, viaHttp.StatusCode);
    }

    [Fact]
    public async Task WithHandlers_DerivesTheDescriptorEagerlyAndStartsAnnouncingBeforeAnyRequest()
    {
        // A real loopback listener standing in for the collector: WithHandlers means the
        // descriptor is built at wire-up (not on first invocation), so registration should reach
        // the collector even though this test never sends any request to the service itself.
        var port = GetFreeTcpPort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/benzene/invoke/");
        listener.Start();
        var receiveRegisterAsync = Task.Run(async () =>
        {
            var context = await listener.GetContextAsync();
            using var reader = new StreamReader(context.Request.InputStream);
            var body = await reader.ReadToEndAsync();
            context.Response.StatusCode = 200;
            context.Response.OutputStream.Close();
            return body;
        });

        CreateHost(cloud => cloud
            .WithHandlers(typeof(ExampleMessageHandler))
            .WithCollector($"http://localhost:{port}/benzene/invoke"));

        var completed = await Task.WhenAny(receiveRegisterAsync, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(receiveRegisterAsync, completed);
        Assert.Contains("mesh:register", await receiveRegisterAsync);
    }

    [Fact]
    public async Task WithMiddleware_RunsInsideTheEnvelopePipeline_BeforeTheRouter()
    {
        var host = CreateHost(cloud => cloud.WithMiddleware(envelope => envelope.Use(
            _ => new FuncWrapperMiddleware<BenzeneMessageContext>("Test", (context, next) =>
            {
                if (context.BenzeneMessageRequest.Topic != "custom-intercept")
                {
                    return next();
                }
                context.BenzeneMessageResponse.StatusCode = "ok";
                context.BenzeneMessageResponse.Body = "{\"intercepted\":true}";
                return Task.CompletedTask;
            }))));

        var response = await host.SendApiGatewayAsync(
            HttpBuilder.Create("POST", CloudServicePaths.Invoke, CreateEnvelope("custom-intercept")));

        Assert.Equal(200, response.StatusCode);
        Assert.Contains("intercepted", response.Body);
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [Fact]
    public void ProfileReport_EvaluatesTheWiringHonestly()
    {
        var report = BuildReport(cloud => cloud.WithCollector("http://collector/benzene/invoke"));
        Assert.True(report.IsConformant);
        Assert.Empty(report.Missing);

        var noCollector = BuildReport(null);
        Assert.False(noCollector.IsConformant);
        Assert.Equal(new[] { "R6" }, noCollector.Missing);

        var withoutMesh = BuildReport(cloud => cloud.WithoutMesh());
        Assert.Equal(new[] { "R6", "R8" }, withoutMesh.Missing);

        var relocated = BuildReport(cloud => cloud
            .WithCollector("http://collector/benzene/invoke")
            .WithSpecPath("/spec"));
        Assert.Equal(new[] { "R7" }, relocated.Missing);
    }

    private static CloudServiceProfileReport BuildReport(Action<ICloudServiceBuilder>? configure)
    {
        CloudServiceProfileReport? report = null;
        new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseBenzeneCloudService("orders", cloud =>
                    {
                        configure?.Invoke(cloud);
                        cloud.WithProfileReport(x => report = x);
                    })
                )
            )
            .BuildHost();
        return report!;
    }
}
