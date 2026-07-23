using Benzene.Abstractions.Hosting;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Benzene.SelfHost.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BenzeneStarter;

// DEPRECATED: this template hosts HTTP on Benzene.SelfHost.Http (System.Net.HttpListener), which is
// materially slower than Kestrel. For a production HTTP service use the `benzene.asp` template
// (Benzene.AspNet.Core / Kestrel) instead - your handlers and StartUp move across unchanged. See
// docs/deprecations.md. This template is kept only as a minimal no-ASP.NET reference.
public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // UseHttp (below) registers the HTTP routing/mapper services for you, so ConfigureServices only
        // needs your handlers + diagnostics. AddDiagnostics() wraps every middleware in an Activity span
        // (a no-op until an OpenTelemetry exporter is attached); the generic host wires console logging.
        services.UsingBenzene(x => x
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddDiagnostics());
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        // A self-hosted HTTP listener (System.Net.HttpListener, not Kestrel). Routes each request to a
        // handler by the method + path its [HttpEndpoint(...)] declares. Point Url at 0.0.0.0 to accept
        // remote connections; ConcurrentRequests bounds in-flight work (backpressure). Once running:
        //   curl http://localhost:8080/hello/world
        var httpConfig = new BenzeneHttpConfig
        {
            Url = "http://localhost:8080/",
            ConcurrentRequests = 5,
        };

        // UseBenzeneEnrichment + UseLogResult give day-one visibility: a structured log line per request
        // (Info on success, Error on a thrown exception) tagged topic/transport/handler. To also turn a
        // thrown exception into a response, add .UseExceptionHandler((ctx, ex) => ...) - see
        // docs/diagnosing-failures.md and docs/cookbooks/global-error-handling.md.
        app.UseWorker(worker => worker
            .UseHttp(httpConfig, http => http
                .UseBenzeneEnrichment()
                .UseLogResult(_ => { })
                .UseMessageHandlers()));
    }
}
