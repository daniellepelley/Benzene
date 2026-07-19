using System.Reflection;
using Amazon.SQS;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Aws.Lambda.EventBridge;
using Benzene.Aws.Lambda.Sns;
using Benzene.Aws.Lambda.Sqs;
using Benzene.CloudService;
using Benzene.Clients;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Abstractions.Messages;
using Benzene.Diagnostics;
using Benzene.Diagnostics.Correlation;
using Benzene.Extras.Broadcast;
using Benzene.FluentValidation;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Benzene.OpenTelemetry;
using Benzene.Schema.OpenApi;
using Benzene.Spec.Ui;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Benzene.Examples.AwsMesh.Shared;

/// <summary>
/// The shared "go to town" wiring for an AwsMesh Cloud Service: the full Cloud Service Profile over
/// HTTP, direct-invoke interrogation, <b>and the same domain handlers exposed over SQS, SNS and
/// EventBridge</b> — every pipeline wrapped with structured logging + correlation IDs and every
/// message validated with FluentValidation. Each service (orders/payments/shipping) supplies only its
/// own domain: its handler types, health checks, and validators.
/// </summary>
public static class MeshServiceWiring
{
    /// <summary>
    /// Registers the baseline, the domain handlers, HTTP routing, JSON console logging (captured to
    /// CloudWatch on Lambda), and the domain's FluentValidation validators. Each
    /// <paramref name="outboundSends"/> both <b>declares</b> a topic this service sends (so it appears
    /// in the spec's <c>events</c> → the mesh's structural topology) <b>and wires the runtime route</b>
    /// (an <see cref="IBenzeneMessageSender"/> that fans the topic out to the SQS queue named by the
    /// send's env var — the ingress the target service already consumes).
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, string serviceName, Assembly domainAssembly, params OutboundSend[] outboundSends)
    {
        // Structured JSON logs to stdout → CloudWatch. The correlation id + processTime that
        // UseLogResult emits ride along on every line.
        services.AddLogging(logging => logging.AddJsonConsole());
        services.AddValidatorsFromAssembly(domainAssembly);

        // Full OpenTelemetry: Benzene's traces + metrics (the pipeline emits spans/metrics via the
        // UseW3CTraceContext/UseBenzeneEnrichment/UseBenzeneMetrics middleware in Configure). The OTLP
        // exporter is only attached when OTEL_EXPORTER_OTLP_ENDPOINT is set — otherwise the instrumentation
        // is still armed (set the env var and it exports) but nothing tries to reach a collector, so there
        // are no connection-refused errors in CloudWatch when running without one.
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService($"{serviceName}-api"))
            .WithTracing(tracing =>
            {
                tracing.SetSampler(new AlwaysOnSampler()).AddBenzeneInstrumentation();
                if (!string.IsNullOrEmpty(otlpEndpoint)) tracing.AddOtlpExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddBenzeneInstrumentation();
                if (!string.IsNullOrEmpty(otlpEndpoint)) metrics.AddOtlpExporter();
            });

        services.UsingBenzene(x =>
        {
            x.AddBenzene()
                .AddCorrelationId()
                // AddDiagnostics() wires ActivityMiddlewareWrapper, so every middleware in every
                // pipeline turns up as its own Activity span (tagged benzene.transport/topic/handler)
                // and is exported over OTLP by AddBenzeneInstrumentation() above — full per-middleware
                // tracing with no per-stage code. (For spans-only, AddActivityPerMiddleware() is the
                // focused opt-in.)
                .AddDiagnostics()
                .AddMessageHandlers(domainAssembly)
                .AddHttpMessageHandlers();

            if (outboundSends.Length > 0)
            {
                // Declare each send in the spec's events → the mesh's structural topology edge.
                x.AddBroadcastEvent(outboundSends
                    .Select(s => (IMessageDefinition)new BroadcastEventDefinition(s.Topic, s.MessageType))
                    .ToArray());

                // The runtime route: an IBenzeneMessageSender that sends each topic to its target
                // service's SQS ingress queue. Lazy IAmazonSQS so the client is only built on a real
                // send (so a service without queue env vars — e.g. the Lambda test tool — still starts).
                x.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient());
                x.AddOutboundRouting(routing =>
                {
                    foreach (var send in outboundSends)
                    {
                        var queueUrl = Environment.GetEnvironmentVariable(send.QueueUrlEnvVar) ?? "";
                        routing.Route(send.Topic, pipeline => pipeline.UseSqs(queueUrl));
                    }
                });
            }
        });
    }

    /// <summary>
    /// Wires every transport onto the AWS Lambda entry point, all routing to the same
    /// <paramref name="handlers"/>, each observable and validated.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="serviceName">The logical service name (e.g. <c>orders</c>).</param>
    /// <param name="handlers">The domain handler types this service exposes.</param>
    /// <param name="healthChecks">The service's health checks.</param>
    public static void Configure(IBenzeneApplicationBuilder app, string serviceName, Type[] handlers, IHealthCheck[] healthChecks)
    {
        var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-1";

        app.UseAwsLambda(aws =>
        {
            // Public HTTP surface: the full Cloud Service Profile + the Spec UI, logged per request.
            aws.UseApiGateway(http => Observe(http)
                .UseSpecUi("/benzene/spec-ui", "/benzene/spec?type=benzene")
                .UseBenzeneCloudService($"{serviceName}-api", cloud => cloud
                    .WithServiceVersion("1.0.0")
                    .WithInstanceId(serviceName)
                    .WithPlacement("aws", region)
                    .WithHealthChecks(healthChecks)
                    .WithHandlers(handlers)));

            // Direct-invoke surface (how the mesh interrogates this Lambda) + the domain handlers.
            aws.UseBenzeneMessage(bm => Observe(bm)
                .UseHealthCheck("healthcheck", healthChecks)
                .UseSpec()
                .UseMessageHandlers(handlers, router => router.UseFluentValidation()));

            // The same domain handlers, now reachable over three more event sources — so you can fire
            // any of them from the Lambda test tool (see .lambda-test-tool/SavedRequests).
            aws.UseSqs(sqs => Observe(sqs)
                .UseMessageHandlers(handlers, router => router.UseFluentValidation()));

            aws.UseSns(sns => Observe(sns)
                .UseMessageHandlers(handlers, router => router.UseFluentValidation()));

            aws.UseEventBridge(eventBridge => Observe(eventBridge)
                .UseMessageHandlers(handlers, router => router.UseFluentValidation()));
        });
    }

    /// <summary>
    /// The observability prelude applied to every transport pipeline: W3C trace-context
    /// extraction/propagation (this is what connects the order → payment → shipment spans across the
    /// SQS hops into one trace), Activity enrichment, per-message metrics, and the structured
    /// correlation-tagged log line.
    /// </summary>
    private static IMiddlewarePipelineBuilder<TContext> Observe<TContext>(IMiddlewarePipelineBuilder<TContext> pipeline)
        => pipeline
            .UseW3CTraceContext()
            .UseBenzeneEnrichment()
            .UseBenzeneMetrics()
            .UseLogResult(log => log.WithCorrelationId());
}
