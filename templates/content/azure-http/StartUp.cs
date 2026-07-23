using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.AspNet;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BenzeneStarter;

public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // AddDiagnostics() wraps every middleware in an Activity span and marks failing stages Error
        // - a no-op until an OpenTelemetry exporter is attached. The Functions host wires logging
        // (Application Insights) for you. See docs/monitoring.md and docs/diagnosing-failures.md.
        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddHttpMessageHandlers()
            .AddDiagnostics());
    }

    // UseHttp is a no-op on any host other than Azure Functions, which is what lets this exact
    // StartUp shape be reused across platforms (see docs/asp-net-core.md). Add
    // .UseEventHub(...)/.UseKafka(...)/.UseServiceBus(...) here too if this Function App should
    // also handle those trigger types - see docs/azure-functions.md.
    //
    // UseBenzeneEnrichment + UseLogResult give day-one visibility: a structured log line per
    // request (Info on success, Error on a thrown exception) tagged topic/transport/handler. To
    // also turn a thrown exception into a response, add .UseExceptionHandler((ctx, ex) => ...) -
    // see docs/diagnosing-failures.md and docs/cookbooks/global-error-handling.md.
    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseHttp(http => http
            .UseBenzeneEnrichment()
            .UseLogResult(_ => { })
            .UseMessageHandlers());
    }
}
