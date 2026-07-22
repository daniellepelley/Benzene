using System;
using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.EventBridge;
using Benzene.Aws.Lambda.XRay;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Diagnostics;
using Benzene.Http.Cors;
using Benzene.Mesh.Aws.Lambda;
using Benzene.Mesh.Aws.S3;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Discovery.Aws;
using Benzene.Mesh.Ui;
using Benzene.Microsoft.Dependencies;
using Benzene.Examples.AwsMesh.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AwsMesh.Mesh;

/// <summary>
/// The mesh service, hosted as an AWS Lambda. On an EventBridge schedule it discovers the
/// benzene-tagged service Lambdas, writes the discovered registry to S3, interrogates each by
/// Lambda-Invoke, and writes the catalog artifacts to S3. Over HTTP (API Gateway) it serves the Mesh
/// UI, the catalog artifacts (read from S3), and an on-demand refresh.
/// </summary>
public class Startup : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var bucket = Environment.GetEnvironmentVariable("MESH_ARTIFACT_BUCKET")
                     ?? throw new InvalidOperationException("MESH_ARTIFACT_BUCKET is required.");
        var prefix = Environment.GetEnvironmentVariable("MESH_ARTIFACT_PREFIX") ?? "";

        // Full OpenTelemetry for the mesh Lambda too, so its discovery/aggregation + UI pipelines are
        // traced and metered alongside the services. Built eagerly (not via services.AddOpenTelemetry())
        // so the providers actually exist under a bare Lambda host, and force-flushed per invocation by
        // TracingLambdaHost — see LambdaTelemetry for why the usual hosting integration records nothing here.
        LambdaTelemetry.Configure(services, "benzene-mesh");

        services.UsingBenzene(benzene =>
        {
            // Baseline every Benzene app needs (IDefaultStatuses, serializer, version selection, core
            // middleware). UseApiGateway/UseEventBridge/UseMessageHandlers don't register it — the app
            // must, same as every other Benzene example.
            benzene.AddBenzene();
            benzene.AddDiagnostics();
            // Also emit an AWS X-Ray subsegment per middleware (nested under the Lambda's X-Ray segment,
            // no OTLP collector needed) alongside the OTel Activity spans AddDiagnostics() produces.
            benzene.AddXRayTracing();
            benzene.AddMessageHandlers(typeof(Startup).Assembly);
            // Discovery starts with an empty registry — discovery replaces it at runtime; artifacts live in S3.
            benzene.AddMeshAggregatorWithS3(new MeshServiceRegistry(Array.Empty<MeshServiceRegistryEntry>()), bucket, prefix);
            benzene.AddMeshLambdaSource();          // LambdaMeshServiceSource: interrogate a service by Invoke
            benzene.AddMeshAwsLambdaDiscovery();    // AwsLambdaDiscoveryProvider + MeshDiscoveryRunner
        });
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        // Scope handler discovery to THIS assembly. The parameterless UseMessageHandlers() scans every
        // loaded assembly, which would also discover Benzene.Mesh.Aggregator's own
        // MeshAggregateMessageHandler — a second handler for topic "mesh:aggregate" (collision). This
        // example deliberately uses its own MeshAggregateHandler (discovery + aggregate) instead.
        var handlers = typeof(Startup).Assembly;

        app.UseAwsLambda(aws =>
        {
            // Scheduled aggregation: an EventBridge rule fires with detail-type "mesh:aggregate".
            aws.UseEventBridge(eb => eb
                .UseW3CTraceContext()
                .UseBenzeneEnrichment()
                .UseBenzeneMetrics()
                .UseMessageHandlers(handlers));

            // Public HTTP surface: the Mesh UI, the catalog artifacts (from S3), and POST /mesh/refresh.
            aws.UseApiGateway(http => http
                .UseW3CTraceContext()
                .UseBenzeneEnrichment()
                .UseBenzeneMetrics()
                .UseMeshUi("/mesh-ui", "manifest.json")
                // The mesh-hosted per-service Spec UI (mesh-ui's "spec" link). Renders each service's
                // spec from the same-origin services/{name}.json snapshot, so a service only serves JSON.
                .UseMeshSpecUi("/mesh-spec-ui.html", "manifest.json")
                // Allow the AsyncAPI Studio deep-link to fetch asyncapi.json cross-origin. Uses
                // Benzene's own CORS support (Benzene.Http.Cors.CorsSettings); "*" would open it to
                // any origin, but scoping to Studio's origin keeps the example tight.
                .UseMeshArtifacts(new CorsSettings { AllowedDomains = new[] { "https://studio.asyncapi.com" } })
                .UseMessageHandlers(handlers));
        });
    }
}

/// <summary>AWS Lambda entry point hosting <see cref="Startup"/>, force-flushing OpenTelemetry per invocation.</summary>
public class Function : TracingLambdaHost<Startup>;
