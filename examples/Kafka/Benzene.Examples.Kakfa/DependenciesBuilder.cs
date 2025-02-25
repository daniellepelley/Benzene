﻿using Benzene.Abstractions.Logging;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Diagnostics.Timers;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Http.Routing;
using Benzene.Kafka.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.Microsoft.Logging;
using Benzene.Schema.OpenApi;
using Benzene.Xml;
using Confluent.Kafka;

namespace Benzene.Examples.Kakfa;

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

    public static ServiceCollection CreateServiceCollection(IConfiguration configuration)
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

        services.AddScoped<IOrderDbClient, InMemoryOrderDbClient>();
        services.AddScoped<IOrderService, OrderService>();

        // services.AddDbContext<DataContext>(x => x.UseNpgsql(configuration["DB_CONNECTION_STRING"],
        //     pgOptions => pgOptions.ProvidePasswordCallback(DbConnectionStringFactory.PasswordCallback())));

        // services.AddScoped<IDataContext>(x => new OrderDataContext(x.GetService<DataContext>()));

        services.UsingBenzene(x => x
            .AddBenzene()
            .AddXml()
            .AddMicrosoftLogger()
            .AddMessageHandlers(typeof(CreateOrderMessage).Assembly)
            .AddKafkaMessageHandlers<Ignore, string>(typeof(CreateOrderMessage).Assembly)
        );
        // services.AddValidatorsFromAssemblyContaining<GetOrderMessageValidator>();
        services.AddScoped<ILogger>(x => x.GetService<ILogger<string>>());
        
        services.AddScoped<IProcessTimerFactory>(x =>
            new CompositeProcessTimerFactory(
                new LoggingProcessTimerFactory(x.GetService<IBenzeneLogger>())));

        services.AddSingleton<IMessageHandlerDefinition>(_ =>
            MessageHandlerDefinition.CreateInstance("spec", "", typeof(SpecRequest), typeof(RawStringMessage), typeof(SpecMessageHandler)));
        services.AddScoped<SpecMessageHandler>();
        services.AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("get", "/spec", "spec"));
    }
}