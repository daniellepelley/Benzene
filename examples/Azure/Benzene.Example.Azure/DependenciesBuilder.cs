using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Clients;
using Benzene.Clients.Azure.ServiceBus;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages;
using Benzene.Diagnostics;
using Benzene.Examples.App.Handlers;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Http;
using Benzene.Http.Routing;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Example.Azure;

public static class DependenciesBuilder
{
    public static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();
    }

    public static IServiceResolverFactory CreateServiceResolverFactory(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        Register(services, configuration);
        return new MicrosoftServiceResolverFactory(services);
    }

    public static void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);

        services.AddScoped<IOrderService, HardcodedOrderService>();
        services.AddSingleton<IMessageHandlerDefinition>(_ =>
            MessageHandlerDefinition.CreateInstance("spec", "", typeof(SpecRequest), typeof(RawStringMessage), typeof(SpecMessageHandler)));
        services.AddScoped<SpecMessageHandler>();
        services.AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("get", "/spec", "spec"));

        // Egress demo (release plan Tier 2.3): reuses the same "ServiceBusConnection" app setting
        // and "orders" queue the ServiceBusFunction ingress trigger already talks to (see
        // ServiceBusFunction.cs), on a distinct topic ("order_created") - see
        // PublishOrderCreatedMessageHandler. Benzene never wraps the connection-string-vs-Managed-
        // Identity choice - build the ServiceBusClient however your deployment needs (see
        // docs/cookbooks/managed-identity.md for the DefaultAzureCredential path).
        var serviceBusClient = new ServiceBusClient(configuration["ServiceBusConnection"]);
        var serviceBusSender = serviceBusClient.CreateSender("orders");
        services.AddSingleton(serviceBusClient);
        services.AddSingleton(serviceBusSender);

        services.UsingBenzene(x => x
                .AddBenzene()
                // Both the shared App domain's handlers AND this host's own (PublishOrderCreatedMessageHandler
                // below) - AddMessageHandlers only registers handlers from the assemblies it's given, and
                // the finder it registers is locked in via TryAddSingleton, so a later broader
                // .UseMessageHandlers(...) scan in Configure() can't widen it - omitting this project's
                // own assembly here left PublishOrderCreatedMessageHandler undiscoverable (no topic
                // route, no HTTP route) despite compiling and looking wired.
                .AddMessageHandlers(typeof(CreateOrderMessage).Assembly, typeof(PublishOrderCreatedMessageHandler).Assembly)
                .AddHttpMessageHandlers()
                // Tags each pipeline stage's Activity and enables UseBenzeneEnrichment() below -
                // combined with the Application Insights logging wired in Program.cs, this is
                // what correlates Benzene's topic/handler/invocationId onto every log line and
                // trace that reaches Application Insights. See docs/cookbooks/logging-application-insights.md.
                .AddDiagnostics()
                .AddOutboundRouting(routing => routing
                    .Route(MessageTopicNames.OrderCreated, pipeline => pipeline.UseServiceBus(serviceBusSender))));
    }
}