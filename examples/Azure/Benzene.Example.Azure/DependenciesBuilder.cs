using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Http;
using Benzene.Http.Routing;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;

namespace Benzene.Example.Azure;

public static class DependenciesBuilder
{
    public static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            // .AddJsonFile("config.json")
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
        // services.AddLogging();
        // services.AddScoped<ILogger, Logger<string>>();
        // services.AddCorrelationId();

        services.AddScoped<IOrderService, HardcodedOrderService>();
        services.AddSingleton<IMessageHandlerDefinition>(_ =>
            MessageHandlerDefinition.CreateInstance("spec", "", typeof(SpecRequest), typeof(RawStringMessage), typeof(SpecMessageHandler)));
        services.AddScoped<SpecMessageHandler>();
        services.AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("get", "/spec", "spec"));
        services.UsingBenzene(x => x
                .AddMessageHandlers(typeof(CreateOrderMessage).Assembly)
                .AddHttpMessageHandlers());
    }
}

// public static class Extensions
// {
//     public static IServiceCollection AddRoute(this IServiceCollection services, string method, string path,
//         string topic)
//     {
//         var t = services.BuildServiceProvider().GetService<IListHttpEndpointFinder>();
//         t.Add(new HttpEndpointDefinition(method, path, topic));
//         return services;
//     }
// }