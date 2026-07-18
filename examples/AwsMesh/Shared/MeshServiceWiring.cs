using System.Reflection;
using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Aws.Lambda.EventBridge;
using Benzene.Aws.Lambda.Sns;
using Benzene.Aws.Lambda.Sqs;
using Benzene.CloudService;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Diagnostics.Correlation;
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
    /// CloudWatch on Lambda), and the domain's FluentValidation validators.
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, Assembly domainAssembly)
    {
        // Structured JSON logs to stdout → CloudWatch. The correlation id + processTime that
        // UseLogResult emits ride along on every line.
        services.AddLogging(logging => logging.AddJsonConsole());
        services.AddValidatorsFromAssembly(domainAssembly);

        services.UsingBenzene(x => x
            .AddBenzene()
            .AddCorrelationId()
            .AddMessageHandlers(domainAssembly)
            .AddHttpMessageHandlers());
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
            aws.UseApiGateway(http => http
                .UseLogResult(log => log.WithCorrelationId())
                .UseSpecUi("/benzene/spec-ui", "/benzene/spec?type=benzene")
                .UseBenzeneCloudService($"{serviceName}-api", cloud => cloud
                    .WithServiceVersion("1.0.0")
                    .WithInstanceId(serviceName)
                    .WithPlacement("aws", region)
                    .WithHealthChecks(healthChecks)
                    .WithHandlers(handlers)));

            // Direct-invoke surface (how the mesh interrogates this Lambda) + the domain handlers.
            aws.UseBenzeneMessage(bm => bm
                .UseLogResult(log => log.WithCorrelationId())
                .UseHealthCheck("healthcheck", healthChecks)
                .UseSpec()
                .UseMessageHandlers(handlers, router => router.UseFluentValidation()));

            // The same domain handlers, now reachable over three more event sources — so you can fire
            // any of them from the Lambda test tool (see .lambda-test-tool/SavedRequests).
            aws.UseSqs(sqs => sqs
                .UseLogResult(log => log.WithCorrelationId())
                .UseMessageHandlers(handlers, router => router.UseFluentValidation()));

            aws.UseSns(sns => sns
                .UseLogResult(log => log.WithCorrelationId())
                .UseMessageHandlers(handlers, router => router.UseFluentValidation()));

            aws.UseEventBridge(eventBridge => eventBridge
                .UseLogResult(log => log.WithCorrelationId())
                .UseMessageHandlers(handlers, router => router.UseFluentValidation()));
        });
    }
}
