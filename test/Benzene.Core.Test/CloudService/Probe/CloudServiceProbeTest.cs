using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Benzene.CloudService.Probe;
using Xunit;

namespace Benzene.Test.CloudService.Probe;

/// <summary>
/// Unit-level tests for <see cref="CloudServiceProbe"/>'s verdict/parsing logic, driven against a
/// real loopback <see cref="HttpListener"/> returning scripted responses - the same pattern used by
/// <c>UseBenzeneCloudServiceTest</c>'s <c>WithHandlers_DerivesTheDescriptorEagerlyAndStartsAnnouncingBeforeAnyRequest</c>
/// - rather than a mocking framework, since this is exercising real HTTP behavior.
/// </summary>
public class CloudServiceProbeTest
{
    [Fact]
    public async Task FullyConformantService_ReportsSatisfiedForEverythingExceptR8AndTheRegistrationHalfOfR6()
    {
        var port = GetFreeTcpPort();
        using var fake = new FakeCloudService(port, CloudServiceProbePaths.Health, CloudServiceProbePaths.Spec, CloudServiceProbePaths.Invoke);
        fake.OnHealth = ctx => WriteJsonAsync(ctx, 200, "{\"isHealthy\":true}");
        fake.OnSpec = ctx => WriteJsonAsync(ctx, 200, "{\"openapi\":\"3.0.0\"}");
        fake.OnInvoke = (ctx, topic) => topic switch
        {
            "healthcheck" => WriteJsonAsync(ctx, 200, Envelope("{\"isHealthy\":true}")),
            "mesh" => WriteJsonAsync(ctx, 200, Envelope("{\"service\":\"orders\",\"topics\":[{\"id\":\"order:create\"}]}")),
            _ => WriteJsonAsync(ctx, 404, "{}")
        };

        using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}") };
        var report = await CloudServiceProbe.RunAsync(client);

        AssertVerdict(report, "R1", CloudServiceProbeVerdict.Satisfied);
        AssertVerdict(report, "R2", CloudServiceProbeVerdict.Satisfied);
        AssertVerdict(report, "R3", CloudServiceProbeVerdict.Satisfied);
        AssertVerdict(report, "R4", CloudServiceProbeVerdict.Satisfied);
        AssertVerdict(report, "R5", CloudServiceProbeVerdict.Satisfied);
        AssertVerdict(report, "R6", CloudServiceProbeVerdict.Satisfied);
        AssertVerdict(report, "R7", CloudServiceProbeVerdict.Satisfied);
        AssertVerdict(report, "R8", CloudServiceProbeVerdict.Inconclusive);

        // R6 is Satisfied for the observable descriptor check, but its reason must still say the
        // registration/heartbeat half was never actually verified - a passing descriptor check
        // must not silently imply the whole of R6.
        var r6 = report.Requirements.Single(x => x.Id == "R6");
        Assert.Contains("registration", r6.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot be observed", r6.Reason, StringComparison.OrdinalIgnoreCase);

        // R8 is never observable from one service, no matter how conformant it looks.
        var r8 = report.Requirements.Single(x => x.Id == "R8");
        Assert.Contains("cannot be verified", r8.Reason, StringComparison.OrdinalIgnoreCase);

        Assert.All(report.Requirements, x => Assert.False(string.IsNullOrWhiteSpace(x.Reason)));
    }

    [Fact]
    public async Task HealthEndpointReturns503WithValidBody_R3IsSatisfied()
    {
        // A conformant service that is merely unhealthy at probe time returns 503 with the same
        // health-report body. Runtime degradation is not a conformance failure - R3 must pass on the
        // report shape, not require a 200.
        var port = GetFreeTcpPort();
        using var fake = new FakeCloudService(port, CloudServiceProbePaths.Health, CloudServiceProbePaths.Spec, CloudServiceProbePaths.Invoke);
        fake.OnHealth = ctx => WriteJsonAsync(ctx, 503, "{\"isHealthy\":false}");
        fake.OnSpec = ctx => WriteJsonAsync(ctx, 200, "{\"openapi\":\"3.0.0\"}");
        fake.OnInvoke = (ctx, topic) => topic switch
        {
            "healthcheck" => WriteJsonAsync(ctx, 503, Envelope("{\"isHealthy\":false}")),
            _ => WriteJsonAsync(ctx, 404, "{}")
        };

        using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}") };
        var report = await CloudServiceProbe.RunAsync(client);

        AssertVerdict(report, "R3", CloudServiceProbeVerdict.Satisfied);
    }

    [Fact]
    public async Task HealthEndpointReturnsUnexpectedStatus_R3IsNotSatisfied()
    {
        // A status that is neither 200 nor 503 is not a conformant health response.
        var port = GetFreeTcpPort();
        using var fake = new FakeCloudService(port, CloudServiceProbePaths.Health, CloudServiceProbePaths.Spec, CloudServiceProbePaths.Invoke);
        fake.OnHealth = ctx => WriteJsonAsync(ctx, 500, "{\"isHealthy\":false}");
        fake.OnSpec = ctx => WriteJsonAsync(ctx, 200, "{\"openapi\":\"3.0.0\"}");
        fake.OnInvoke = (ctx, topic) => WriteJsonAsync(ctx, 404, "{}");

        using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}") };
        var report = await CloudServiceProbe.RunAsync(client);

        AssertVerdict(report, "R3", CloudServiceProbeVerdict.NotSatisfied);
    }

    [Fact]
    public async Task UnreachableService_IsNotSatisfiedAndCascadesSensibly()
    {
        // Nothing is listening on this port - every probe call fails at the transport level.
        var port = GetFreeTcpPort();
        using var client = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{port}"),
            Timeout = TimeSpan.FromSeconds(5)
        };

        var report = await CloudServiceProbe.RunAsync(client);

        AssertVerdict(report, "R1", CloudServiceProbeVerdict.NotSatisfied);
        AssertVerdict(report, "R3", CloudServiceProbeVerdict.NotSatisfied);
        AssertVerdict(report, "R4", CloudServiceProbeVerdict.NotSatisfied);
        AssertVerdict(report, "R5", CloudServiceProbeVerdict.NotSatisfied);
        AssertVerdict(report, "R6", CloudServiceProbeVerdict.NotSatisfied);
        // No mesh descriptor was reachable, so there's no evidence either way for the registry.
        AssertVerdict(report, "R2", CloudServiceProbeVerdict.Inconclusive);
        // Default paths were used, but nothing checked out there.
        AssertVerdict(report, "R7", CloudServiceProbeVerdict.NotSatisfied);
        AssertVerdict(report, "R8", CloudServiceProbeVerdict.Inconclusive);
    }

    [Fact]
    public async Task SpecMissing_R5FailsWhileHealthAndInvokePass_AndR2DegradesToInconclusive()
    {
        var port = GetFreeTcpPort();
        using var fake = new FakeCloudService(port, CloudServiceProbePaths.Health, CloudServiceProbePaths.Spec, CloudServiceProbePaths.Invoke);
        fake.OnHealth = ctx => WriteJsonAsync(ctx, 200, "{\"isHealthy\":true}");
        fake.OnSpec = ctx => WriteJsonAsync(ctx, 404, "{}");
        fake.OnInvoke = (ctx, topic) => topic == "healthcheck"
            ? WriteJsonAsync(ctx, 200, Envelope("{\"isHealthy\":true}"))
            : WriteJsonAsync(ctx, 404, "{}"); // the reserved "mesh" topic is not served either

        using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}") };
        var report = await CloudServiceProbe.RunAsync(client);

        AssertVerdict(report, "R3", CloudServiceProbeVerdict.Satisfied);
        AssertVerdict(report, "R4", CloudServiceProbeVerdict.Satisfied);
        AssertVerdict(report, "R5", CloudServiceProbeVerdict.NotSatisfied);
        AssertVerdict(report, "R6", CloudServiceProbeVerdict.NotSatisfied);
        // No mesh descriptor and the spec format isn't assumed - so no evidence about the registry.
        AssertVerdict(report, "R2", CloudServiceProbeVerdict.Inconclusive);
        // Used default paths, but R5/R6 didn't check out there.
        AssertVerdict(report, "R7", CloudServiceProbeVerdict.NotSatisfied);
    }

    [Fact]
    public async Task NonDefaultPathsSupplied_ForcesR7Inconclusive_EvenWhenEverythingElseChecksOut()
    {
        const string healthPath = "/custom/health";
        const string specPath = "/custom/spec";
        const string invokePath = "/custom/invoke";

        var port = GetFreeTcpPort();
        using var fake = new FakeCloudService(port, healthPath, specPath, invokePath);
        fake.OnHealth = ctx => WriteJsonAsync(ctx, 200, "{\"isHealthy\":true}");
        fake.OnSpec = ctx => WriteJsonAsync(ctx, 200, "{\"openapi\":\"3.0.0\"}");
        fake.OnInvoke = (ctx, topic) => topic switch
        {
            "healthcheck" => WriteJsonAsync(ctx, 200, Envelope("{\"isHealthy\":true}")),
            "mesh" => WriteJsonAsync(ctx, 200, Envelope("{\"service\":\"orders\",\"topics\":[]}")),
            _ => WriteJsonAsync(ctx, 404, "{}")
        };

        using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}") };
        var options = new CloudServiceProbeOptions
        {
            HealthPath = healthPath,
            SpecPath = specPath,
            InvokePath = invokePath
        };

        var report = await CloudServiceProbe.RunAsync(client, options);

        AssertVerdict(report, "R3", CloudServiceProbeVerdict.Satisfied);
        AssertVerdict(report, "R4", CloudServiceProbeVerdict.Satisfied);
        AssertVerdict(report, "R5", CloudServiceProbeVerdict.Satisfied);
        AssertVerdict(report, "R6", CloudServiceProbeVerdict.Satisfied);
        // Every probed surface checked out, but the probe was told to look elsewhere - it still
        // can't claim to know the service's *own* defaults are /benzene/*.
        AssertVerdict(report, "R7", CloudServiceProbeVerdict.Inconclusive);
        var r7 = report.Requirements.Single(x => x.Id == "R7");
        Assert.Contains("non-default", r7.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendTraceParentProbeDisabled_R8StaysInconclusiveWithoutTheBonusSignal()
    {
        var port = GetFreeTcpPort();
        using var fake = new FakeCloudService(port, CloudServiceProbePaths.Health, CloudServiceProbePaths.Spec, CloudServiceProbePaths.Invoke);
        fake.OnHealth = ctx => WriteJsonAsync(ctx, 200, "{\"isHealthy\":true}");
        fake.OnSpec = ctx => WriteJsonAsync(ctx, 200, "{\"openapi\":\"3.0.0\"}");
        fake.OnInvoke = (ctx, topic) => topic switch
        {
            "healthcheck" => WriteJsonAsync(ctx, 200, Envelope("{\"isHealthy\":true}")),
            "mesh" => WriteJsonAsync(ctx, 200, Envelope("{\"service\":\"orders\",\"topics\":[]}")),
            _ => WriteJsonAsync(ctx, 404, "{}")
        };

        using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}") };
        var report = await CloudServiceProbe.RunAsync(client, new CloudServiceProbeOptions { SendTraceParentProbe = false });

        AssertVerdict(report, "R8", CloudServiceProbeVerdict.Inconclusive);
        var r8 = report.Requirements.Single(x => x.Id == "R8");
        Assert.DoesNotContain("bonus signal", r8.Reason, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertVerdict(CloudServiceProbeReport report, string id, CloudServiceProbeVerdict expected)
    {
        var requirement = report.Requirements.SingleOrDefault(x => x.Id == id);
        Assert.NotNull(requirement);
        Assert.True(expected == requirement!.Verdict,
            $"{id}: expected {expected} but was {requirement.Verdict} ({requirement.Reason})");
    }

    private static string Envelope(string body)
    {
        var escaped = JsonSerializer.Serialize(body);
        return $"{{\"statusCode\":\"ok\",\"headers\":{{}},\"body\":{escaped}}}";
    }

    private static async Task WriteJsonAsync(HttpListenerContext context, int statusCode, string json)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var bytes = Encoding.UTF8.GetBytes(json);
        await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        context.Response.OutputStream.Close();
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>A minimal scripted HTTP server for exercising <see cref="CloudServiceProbe"/> against real sockets.</summary>
    private sealed class FakeCloudService : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loop;
        private readonly string _healthPath;
        private readonly string _specPath;
        private readonly string _invokePath;

        public Func<HttpListenerContext, Task>? OnHealth { get; set; }
        public Func<HttpListenerContext, Task>? OnSpec { get; set; }
        public Func<HttpListenerContext, string, Task>? OnInvoke { get; set; }

        public FakeCloudService(int port, string healthPath, string specPath, string invokePath)
        {
            _healthPath = healthPath;
            _specPath = specPath;
            _invokePath = invokePath;

            _listener = new HttpListener();
            foreach (var path in new[] { healthPath, specPath, invokePath }.Distinct())
            {
                _listener.Prefixes.Add($"http://localhost:{port}{path}/");
            }
            _listener.Start();
            _loop = Task.Run(RunAsync);
        }

        private async Task RunAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }
                _ = HandleAsync(context);
            }
        }

        private async Task HandleAsync(HttpListenerContext context)
        {
            try
            {
                var path = context.Request.Url!.AbsolutePath;
                if (path == _healthPath && OnHealth != null)
                {
                    await OnHealth(context);
                }
                else if (path == _specPath && OnSpec != null)
                {
                    await OnSpec(context);
                }
                else if (path == _invokePath && OnInvoke != null)
                {
                    using var reader = new StreamReader(context.Request.InputStream);
                    var body = await reader.ReadToEndAsync();
                    var topic = ExtractTopic(body);
                    await OnInvoke(context, topic);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.OutputStream.Close();
                }
            }
            catch (Exception)
            {
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.OutputStream.Close();
                }
                catch (Exception)
                {
                    // Best-effort only; the client-side probe call will observe the failure either way.
                }
            }
        }

        private static string ExtractTopic(string body)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("topic", out var topic) ? topic.GetString() ?? "" : "";
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            _listener.Close();
        }
    }
}
