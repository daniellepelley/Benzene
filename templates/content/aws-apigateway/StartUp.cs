using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        // AddConsole() so ILogger output reaches CloudWatch (a Lambda host wires no provider by
        // default). AddDiagnostics() wraps every middleware in an Activity span and marks failing
        // stages Error - a no-op until an OpenTelemetry exporter is attached. See
        // docs/monitoring.md and docs/diagnosing-failures.md.
        services.AddLogging(x => x.AddConsole());
        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddHttpMessageHandlers()
            .AddDiagnostics());
    }

    // This is the one place that's specific to API Gateway - wrap this in app.UseAwsLambda(...) with
    // .UseSqs(...)/.UseSns(...)/.UseKafka(...) etc. alongside .UseApiGateway(...) if this function
    // should also handle other AWS event sources. See docs/getting-started-aws.md.
    //
    // UseBenzeneEnrichment + UseLogResult give day-one visibility: a structured log line per
    // request (Info on success, Error on a thrown exception) tagged topic/transport/handler. Benzene
    // already maps unsuccessful *results* to HTTP status codes; to also turn a thrown exception into
    // a response, add .UseExceptionHandler((ctx, ex) => ...) - see docs/diagnosing-failures.md and
    // docs/cookbooks/global-error-handling.md.
    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseAwsLambda(eventPipeline => eventPipeline
            .UseApiGateway(apiGatewayApp => apiGatewayApp
                .UseBenzeneEnrichment()
                .UseLogResult(_ => { })
                .UseMessageHandlers()));
    }
}
