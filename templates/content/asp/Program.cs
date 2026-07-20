using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Microsoft.Dependencies;
using BenzeneStarter;

var builder = WebApplication.CreateBuilder(args);

// Register Benzene and discover message handlers in this assembly. Add more handler classes
// alongside HelloWorldMessageHandler.cs - they're found automatically by reflection, no routing
// table to maintain. AddDiagnostics() wraps every middleware in an Activity span (marking failing
// stages Error) - a no-op until you attach an OpenTelemetry exporter, so it's safe to leave on.
builder.Services.UsingBenzene(x => x
    .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
    .AddDiagnostics());

var app = builder.Build();

// Add the Benzene HTTP pipeline: turn each request into a message and route it to a handler.
// This is the transport-specific wiring - it's the one part that changes if you move this
// handler to AWS Lambda or Azure Functions later (see docs/getting-started-aws.md /
// docs/azure-functions.md).
//
// UseBenzeneEnrichment + UseLogResult give you day-one visibility: a structured log line per
// message (Info on success, Error on a thrown exception) tagged with topic/transport/handler.
// To also map thrown exceptions to a response, add .UseExceptionHandler(...) - see
// docs/diagnosing-failures.md and docs/cookbooks/global-error-handling.md.
app.UseBenzene(benzene => benzene
    .UseHttp(http => http
        .UseBenzeneEnrichment()
        .UseLogResult(_ => { })
        .UseMessageHandlers()));

app.Run();
