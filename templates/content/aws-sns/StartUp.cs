using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Sns;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
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

        // Register your application services here - a test can override any of them (see
        // BenzeneStarter.Tests). IGreeter is the demo handler's one dependency.
        services.AddSingleton<IGreeter, ConsoleGreeter>();

        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddDiagnostics());
    }

    // This is the one place that's specific to SNS - wrap this in app.UseAwsLambda(...) with
    // .UseApiGateway(...)/.UseSqs(...)/.UseKafka(...) etc. alongside .UseSns(...) if this function
    // should also handle other AWS event sources. See docs/getting-started-aws.md.
    //
    // UseBenzeneEnrichment + UseLogResult give day-one visibility: a structured log line per
    // message (Info on success, Error on a thrown exception) tagged topic/transport/handler. To
    // also settle a thrown exception yourself, add .UseExceptionHandler((ctx, ex) => ...) - see
    // docs/diagnosing-failures.md and docs/cookbooks/global-error-handling.md.
    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseAwsLambda(eventPipeline => eventPipeline
            .UseSns(snsApp => snsApp
                .UseBenzeneEnrichment()
                .UseLogResult(_ => { })
                .UseMessageHandlers()));
    }
}
