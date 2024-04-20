using System.IO;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Response;
using Benzene.Azure.Core;
using Benzene.Azure.Core.AspNet;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.DI;
using Benzene.Core.Request;
using Benzene.Core.Response;
using Benzene.Core.Serialization;
using Benzene.Diagnostics.Timers;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Examples.App.Validators;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Benzene.Xml;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        services.AddLogging();
        services.AddScoped<ILogger, Logger<string>>();
        // services.AddStructuredLogging();
        // services.AddCorrelationId();

        // services.AddScoped<IOrderDbClient, OrderDbClient>();
        // services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderService, HardcodedOrderService>();

        // services.AddDbContext<DataContext>(x => x.UseNpgsql(configuration["DB_CONNECTION_STRING"],
            // pgOptions => pgOptions.ProvidePasswordCallback(DbConnectionStringFactory.PasswordCallback())));

        // services.AddScoped<IDataContext>(x => new OrderDataContext(x.GetService<DataContext>()));

        services.UsingBenzene(x => x
                .AddMessageHandlers(typeof(CreateOrderMessage).Assembly)
                .AddHttpMessageHandlers(typeof(CreateOrderMessage).Assembly)
                .AddAzureMessageHandlers());
            
        services.AddScoped<IRequestEnricher<AspNetContext>, AspNetContextRequestEnricher>();
        services.AddValidatorsFromAssemblyContaining<GetOrderMessageValidator>();
        services.AddScoped<IResponseHandler<AspNetContext>, ResponseBodyHandler<AspNetContext>>();
        services.AddScoped<IResponseHandler<AspNetContext>, HttpStatusCodeResponseHandler<AspNetContext>>();

        services.AddScoped<IResponseHandler<AspNetContext>, ResponseHandler<JsonSerializationResponseHandler<AspNetContext>, AspNetContext>>();
        services.AddScoped<IResponseHandler<AspNetContext>, ResponseHandler<XmlSerializationResponseHandler<AspNetContext>, AspNetContext>>();
        services.AddScoped<IResponseHandler<AspNetContext>, HttpStatusCodeResponseHandler<AspNetContext>>();
        
        services.AddScoped<JsonSerializationResponseHandler<AspNetContext>>();
        services.AddScoped<XmlSerializationResponseHandler<AspNetContext>>();

        services.AddScoped<JsonSerializer>();

        services.AddScoped<IProcessTimerFactory, NullProcessTimerFactory>();
        services.AddScoped<IRouteFinder, RouteFinder>();

        // services.AddScoped<IProcessTimerFactory>(x =>
        // new CompositeProcessTimerFactory(
        // new LoggingProcessTimerFactory(x.GetService<ILogger>())));

    }
}