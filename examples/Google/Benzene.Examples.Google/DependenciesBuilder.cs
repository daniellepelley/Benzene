using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Response;
using Benzene.AspNet.Core;
using Benzene.Core.DI;
using Benzene.Diagnostics.Timers;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Examples.App.Validators;
using Benzene.Microsoft.Dependencies;
using Benzene.Microsoft.Logging;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Benzene.Examples.Google;

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

    public static IServiceCollection CreateServiceResolverFactory(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        Register(services, configuration);
        return services;
    }

    public static void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddLogging();
        // services.AddStructuredLogging();
        // services.AddCorrelationId();

        services.AddScoped<ILogger, Logger<string>>();
        // services.AddScoped<IOrderDbClient, OrderDbClient>();
        services.AddScoped<IOrderDbClient, InMemoryOrderDbClient>();
        services.AddScoped<IOrderService, OrderService>();
        // services.AddScoped<AspNetContextResponseMiddleware>();
        // services.AddScoped<IHandlerMiddlewareBuilder, ValidationMiddlewareBuilder>();

        // services.AddDbContext<DataContext>(x => x.UseNpgsql(configuration["DB_CONNECTION_STRING"],
        //     pgOptions => pgOptions.ProvidePasswordCallback(DbConnectionStringFactory.PasswordCallback())));
        //
        // services.AddScoped<IDataContext>(x => new OrderDataContext(x.GetService<DataContext>()));
        services
            .UsingBenzene(x => x
                .AddMicrosoftLogger()
                .AddMessageHandlers(typeof(CreateOrderMessage).Assembly)
                .AddHttpMessageHandlers(typeof(CreateOrderMessage).Assembly)
                .AddAspNetMessageHandlers());

        services.AddValidatorsFromAssemblyContaining<GetOrderMessageValidator>();
        services.AddScoped<IResponsePayloadMapper<AspNetContext>, Custom.CustomResponsePayloadMapper<AspNetContext>>();

        services.AddScoped<IProcessTimerFactory>(x =>
            new CompositeProcessTimerFactory(
                new LoggingProcessTimerFactory(x.GetService<IBenzeneLogger>()),
                new DebugTimerFactory()
            ));
    }
}