using System.IO;
using Benzene.Abstractions.DI;
using Benzene.Core.DI;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
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

        services.UsingBenzene(x => x
                .AddMessageHandlers(typeof(CreateOrderMessage).Assembly)
                .AddHttpMessageHandlers());
    }
}