using System.Reflection;
using Amazon.EventBridge;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Aws.Lambda.EventBridge;
using Benzene.Aws.Lambda.Sns;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Aws.Lambda.XRay;
using Benzene.CloudService;
using Benzene.Clients;
using Benzene.Clients.Aws.EventBridge;
using Benzene.Clients.Aws.Sns;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.MessageHandlers.WarmUp;
using Benzene.Core.Middleware;
using Benzene.Abstractions.Messages;
using Benzene.Diagnostics;
using Benzene.Diagnostics.Correlation;
using Benzene.ResponseEvents;
using Benzene.FluentValidation;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;
using Benzene.Spec.Ui;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        // UseW3CTraceContext/UseBenzeneEnrichment/UseBenzeneMetrics middleware in Configure). Built
        // eagerly (not via services.AddOpenTelemetry()) so the providers actually exist under a bare
        // Lambda host — and force-flushed per invocation by TracingLambdaHost. See LambdaTelemetry for
        // why the usual hosting integration silently records nothing here.
        LambdaTelemetry.Configure(services, $"{serviceName}-api");

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
                // AddXRayTracing() ALSO wraps every middleware, but in an AWS X-Ray subsegment via the
                // X-Ray SDK, so each stage nests directly under the Lambda's X-Ray segment — the
                // middleware breakdown shows up inside the same X-Ray trace as the AWS-level segments,
                // with no OTLP collector in between. Wired alongside AddDiagnostics() so this service
                // emits both X-Ray subsegments and OTel spans; drop either to pick one backend.
                .AddXRayTracing()
                .AddMessageHandlers(domainAssembly)
                .AddHttpMessageHandlers()
                // Opt into cold-start warm-up: at Lambda INIT (AwsLambdaHost's ctor calls WarmUp()) this
                // pre-builds each handler's request+response STJ metadata and each FluentValidation rule
                // set, so the first real invocation doesn't pay that JIT. Invisible - no message dispatch,
                // no logs/metrics/traces. See README "Cold-start tuning".
                .AddBenzeneWarmUp();

            if (outboundSends.Length > 0)
            {
                // Declare each send in the spec's events → the mesh's structural topology edge. This is
                // transport-agnostic on purpose: an SQS command, an SNS event and an EventBridge event all
                // surface the same way, so the mesh topology shows every edge regardless of how it's carried.
                x.AddResponseEventDeclarations(outboundSends
                    .Select(s => (IMessageDefinition)new ResponseEventDefinition(s.Topic, s.MessageType))
                    .ToArray());

                // Only register the AWS clients for the transports this service actually sends over. Lazy
                // factories so a client is only built on a real send — a service with no target env vars
                // (e.g. the Lambda test tool) still starts.
                if (outboundSends.Any(s => s.Transport == OutboundTransport.Sqs))
                {
                    x.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient());
                }
                if (outboundSends.Any(s => s.Transport == OutboundTransport.Sns))
                {
                    x.AddSingleton<IAmazonSimpleNotificationService>(_ => new AmazonSimpleNotificationServiceClient());
                }
                if (outboundSends.Any(s => s.Transport == OutboundTransport.EventBridge))
                {
                    x.AddSingleton<IAmazonEventBridge>(_ => new AmazonEventBridgeClient());
                }

                // The runtime route: one IBenzeneMessageSender pipeline per topic, each publishing over the
                // send's chosen transport to the target the env var names (the ingress the target already
                // consumes). Every transport is fire-and-acknowledge here — the handlers send
                // SendAsync<T, Void>.
                x.AddOutboundRouting(routing =>
                {
                    foreach (var send in outboundSends)
                    {
                        var target = Environment.GetEnvironmentVariable(send.TargetEnvVar) ?? "";
                        routing.Route(send.Topic, pipeline =>
                        {
                            switch (send.Transport)
                            {
                                case OutboundTransport.Sqs:
                                    pipeline.UseSqs(target);
                                    break;
                                case OutboundTransport.Sns:
                                    pipeline.UseSns(target);
                                    break;
                                case OutboundTransport.EventBridge:
                                    // Source = the sending service (matches its OTel service name); the
                                    // bus name comes from the env var. EventBridge rules route on detail-type
                                    // (the Benzene topic) + source.
                                    pipeline.UseEventBridge($"{serviceName}-api", eventBusName: target);
                                    break;
                            }
                        });
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
