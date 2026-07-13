using System.Diagnostics;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Examples.OpenTelemetry;
using Benzene.Microsoft.Dependencies;
using Benzene.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Export Benzene's "Benzene" ActivitySource/Meter plus this example's own source via OTLP.
// The default OTLP endpoint (http://localhost:4317) matches grafana/otel-lgtm's gRPC port.
// SetSampler is required here: without it, no spans are recorded (StartActivity returns null)
// under this OpenTelemetry SDK/.NET combination.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("benzene-otel-example"))
    .WithTracing(t => t
        .SetSampler(new AlwaysOnSampler())
        .AddBenzeneInstrumentation()
        .AddSource(ExampleDiagnostics.SourceName)
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddBenzeneInstrumentation()
        .AddOtlpExporter());

var benzeneContainer = new MicrosoftBenzeneServiceContainer(builder.Services);

var pipelineBuilder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(benzeneContainer);
pipelineBuilder
    .UseW3CTraceContext()
    .UseBenzeneEnrichment()
    .UseBenzeneMetrics()
    .UseMessageHandlers(typeof(Program).Assembly);

var benzeneApplication = new BenzeneMessageApplication(pipelineBuilder.Build());

builder.Services.UsingBenzene(x => x
    .AddBenzene()
    .AddBenzeneMessage()
    .AddMessageHandlers(typeof(Program).Assembly)
    .AddDiagnostics());

builder.Services.AddScoped<IWarehouseService, WarehouseService>();

var app = builder.Build();

var serviceResolverFactory = new MicrosoftServiceResolverFactory(app.Services);

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/topics", (IMessageHandlerDefinitionLookUp lookUp) =>
    lookUp.GetAllHandlers().Select(h => new
    {
        topic = h.Topic.Id,
        version = h.Topic.Version,
        handler = h.HandlerType.Name,
        requestType = h.RequestType.Name
    }));

app.MapPost("/api/send", async (SendMessageRequest send) =>
{
    // Root span from our own source so the UI can show one trace id covering the whole
    // Benzene pipeline (which nests under Activity.Current).
    using var activity = ExampleDiagnostics.ActivitySource.StartActivity(
        $"Send {send.Topic}", ActivityKind.Server);
    activity?.SetTag("benzene.topic", send.Topic);

    var response = await benzeneApplication.HandleAsync(new BenzeneMessageRequest
    {
        Topic = send.Topic,
        Body = send.Body ?? "{}",
        Headers = send.Headers ?? new Dictionary<string, string>()
    }, serviceResolverFactory);

    return Results.Json(new
    {
        statusCode = response.StatusCode,
        body = response.Body,
        headers = response.Headers,
        traceId = activity?.TraceId.ToHexString()
    });
});

app.Run();

public record SendMessageRequest(string Topic, string? Body, Dictionary<string, string>? Headers);
