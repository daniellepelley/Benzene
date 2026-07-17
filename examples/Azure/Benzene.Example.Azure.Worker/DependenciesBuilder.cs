using Benzene.Core.MessageHandlers.DI;
using Benzene.Diagnostics.Timers;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Microsoft.Dependencies;

namespace Benzene.Example.Azure.Worker;

/// <summary>
/// Builds configuration and registers services for the worker - the same shape the Kafka worker
/// example uses, minus the HTTP/spec surface, since this example is consume-only.
/// </summary>
public static class DependenciesBuilder
{
    public static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json")
            .AddEnvironmentVariables()
            .Build();
    }

    public static void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddLogging();

        // The example's business logic - shared with every other example via Benzene.Examples.App.
        services.AddScoped<IOrderDbClient, InMemoryOrderDbClient>();
        services.AddScoped<IOrderService, OrderService>();

        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(CreateOrderMessage).Assembly));

        // Log how long each message takes to handle, so `dotnet run` shows the pipeline firing.
        services.AddScoped<IProcessTimerFactory>(x =>
            new CompositeProcessTimerFactory(
                new LoggingProcessTimerFactory(x.GetService<ILogger<LoggingProcessTimer>>())));
    }
}
