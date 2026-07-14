using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages;
using Benzene.Diagnostics;
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
        services.UsingBenzene(x => x
                .AddMessageHandlers(typeof(CreateOrderMessage).Assembly)
                .AddHttpMessageHandlers()
                // Tags each pipeline stage's Activity and enables UseBenzeneEnrichment() below -
                // combined with the Application Insights logging wired in Program.cs, this is
                // what correlates Benzene's topic/handler/invocationId onto every log line and
                // trace that reaches Application Insights. See docs/cookbooks/logging-application-insights.md.
                .AddDiagnostics());
    }
}