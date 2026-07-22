using Azure;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.AspNet;
using Benzene.CloudService;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Diagnostics;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AzureFunctionsMesh.Shared;

/// <summary>
/// The shared wiring every AzureFunctionsMesh Cloud Service uses: the full Cloud Service Profile over
/// HTTP (how the mesh interrogates it — <c>/benzene/spec</c>, <c>/benzene/health</c>, <c>/benzene/invoke</c>),
/// plus hooks for each service to add the inbound message triggers (Service Bus / Event Hub / Event Grid)
/// and outbound routing it needs. Each service (orders/payments/…) is its own Function App project so it
/// declares exactly the triggers it uses; this holds only the parts identical across all of them.
/// </summary>
public static class MeshServiceWiring
{
    /// <summary>
    /// Registers the baseline Benzene service: the handlers, HTTP message handlers, diagnostics, and the
    /// service's own spec identity. <paramref name="configureBenzene"/> lets a service add its outbound
    /// routing (and register the Azure SDK clients those routes send through).
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, string serviceName, Type[] handlers,
        Action<IBenzeneServiceContainer>? configureBenzene = null)
    {
        services.UsingBenzene(x =>
        {
            x.AddBenzene()
                // Name the service in its own derived spec (the spec title = IApplicationInfo.Name) and
                // under the same name the mesh discovers it by.
                .SetApplicationInfo(serviceName, "1.0.0", $"{serviceName} service")
                .AddDiagnostics()
                .AddMessageHandlers(handlers)
                .AddHttpMessageHandlers();

            configureBenzene?.Invoke(x);
        });
    }

    /// <summary>
    /// Wires the HTTP Cloud Service Profile (the mesh's interrogation surface). <paramref name="configureIngress"/>
    /// lets a service add its message-trigger pipelines (<c>app.UseServiceBus(...)</c> etc.), all routing to
    /// the same <paramref name="handlers"/>.
    /// </summary>
    public static void Configure(IBenzeneApplicationBuilder app, string serviceName, Type[] handlers,
        IHealthCheck[] healthChecks, Action<IBenzeneApplicationBuilder>? configureIngress = null)
    {
        var region = Environment.GetEnvironmentVariable("REGION_NAME") ?? "local";

        // A Cloud Service serves only JSON (spec/health/invoke); the browsable spec is served by the mesh.
        app.UseHttp(http => http
            .UseBenzeneEnrichment()
            .UseBenzeneCloudService($"{serviceName}-api", cloud => cloud
                .WithServiceVersion("1.0.0")
                .WithInstanceId(serviceName)
                .WithPlacement("azure-functions", region)
                .WithHealthChecks(healthChecks)
                .WithHandlers(handlers)));

        configureIngress?.Invoke(app);
    }

    /// <summary>
    /// Registers a lazy <see cref="ServiceBusSender"/> for <paramref name="queueName"/> off the
    /// <c>ServiceBusConnection</c> app setting — created on first send (via <c>UseServiceBusClient()</c>),
    /// so a service still starts locally with no connection configured (the send just fails and is logged).
    /// </summary>
    public static void AddServiceBusSender(this IBenzeneServiceContainer services, string queueOrTopicName)
    {
        services.AddSingleton(_ =>
        {
            var connection = Environment.GetEnvironmentVariable("ServiceBusConnection") ?? "";
            return new ServiceBusClient(connection).CreateSender(queueOrTopicName);
        });
    }

    /// <summary>
    /// Registers a lazy <see cref="EventHubProducerClient"/> for <paramref name="eventHubName"/> off the
    /// <c>EventHubConnection</c> app setting — created on first send, so the service still starts locally
    /// with no connection configured.
    /// </summary>
    public static void AddEventHubProducer(this IBenzeneServiceContainer services, string eventHubName)
    {
        services.AddSingleton(_ =>
        {
            var connection = Environment.GetEnvironmentVariable("EventHubConnection") ?? "";
            return new EventHubProducerClient(connection, eventHubName);
        });
    }

    /// <summary>
    /// Registers a lazy <see cref="EventGridPublisherClient"/> off the <c>EventGridEndpoint</c> +
    /// <c>EventGridKey</c> app settings — created on first send, so the service still starts locally with
    /// no endpoint configured.
    /// </summary>
    public static void AddEventGridPublisher(this IBenzeneServiceContainer services)
    {
        services.AddSingleton(_ =>
        {
            var endpoint = Environment.GetEnvironmentVariable("EventGridEndpoint") ?? "https://localhost";
            var key = Environment.GetEnvironmentVariable("EventGridKey") ?? "";
            return new EventGridPublisherClient(new Uri(endpoint), new AzureKeyCredential(key));
        });
    }
}
