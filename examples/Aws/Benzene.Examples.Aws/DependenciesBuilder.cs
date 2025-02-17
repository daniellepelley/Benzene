using System.IO;
using Amazon;
using Amazon.SQS;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Amazon.Extensions.NETCore.Setup;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Sns;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Diagnostics.Timers;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Logging;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Examples.App.Validators;
using Benzene.Microsoft.Dependencies;
using Benzene.Microsoft.Logging;
using Newtonsoft.Json;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Benzene.Examples.Aws;

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

    public static IServiceResolverFactory CreateServiceResolverFactory(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        Register(services, configuration);
        return new MicrosoftServiceResolverFactory(services);
    }

    public static void Register(IServiceCollection services, IConfiguration configuration)
    {
        JsonConvert.DeserializeObject("{}");
        var awsRegion = configuration.GetValue<string>("AWS_DEFAULT_REGION");
        var awsServiceUrl = configuration.GetValue<string>("AWS_SERVICE_URL");

        var awsOptions = new AWSOptions
        {
            Region = string.IsNullOrWhiteSpace(awsRegion)
                ? RegionEndpoint.EUWest2
                : RegionEndpoint.GetBySystemName(awsRegion),
        };

        if (!string.IsNullOrEmpty(awsServiceUrl))
        {
            awsOptions.DefaultClientConfig.ServiceURL = awsServiceUrl;
        }

        services.AddSingleton(configuration);
        services.AddLogging(x => x.AddConsole().AddSerilog());
        services.AddScoped<ILogger, Logger<string>>();
        services.AddTransient(_ => configuration.GetAWSOptions());
        services.AddSingleton(awsOptions.CreateServiceClient<IAmazonSQS>());
        // services.AddScoped<ISqsClient>(x => new SqsClient(x.GetService<IAmazonSQS>(), configuration["MY_QUEUE_URL"] ));

        services.AddScoped<IOrderDbClient, InMemoryOrderDbClient>();
        services.AddScoped<IOrderService, OrderService>();

        //Custom 
        // services.AddScoped<IResponsePayloadMapper<ApiGatewayContext>, CustomResponsePayloadMapper<ApiGatewayContext>>();
        // services.AddScoped<IResponsePayloadMapper<BenzeneMessageContext>, CustomResponsePayloadMapper<BenzeneMessageContext>>();
        // services.AddScoped<IResponsePayloadMapper<SqsMessageContext>, CustomResponsePayloadMapper<SqsMessageContext>>();
        // services.AddScoped<IResponsePayloadMapper<SnsRecordContext>, CustomResponsePayloadMapper<SnsRecordContext>>();
        //
        
        // services.AddSingleton<ISerializerOption<BenzeneMessageContext>>(new SerializerOption<BenzeneMessageContext, JsonSerializer>(x => true));

        services.AddValidatorsFromAssemblyContaining<GetOrderMessageValidator>();
        services.UsingBenzene(x => x
            .AddBenzene()
            .AddBenzeneMessage()
            .AddStructuredLogging()
            .AddMicrosoftLogger()
            // .AddXml()
            // .AddSerializer<XmlSerializer>("application/xml")
            // .AddCorrelationId()
            .AddMessageHandlers(typeof(CreateOrderMessage).Assembly));
        
        // services.AddScoped<IMiddlewareFactory>(_ => new TimerMiddlewareFactory(
        //     new DebugTimerFactory()
        //     // new XRayProcessTimerFactory()
        //     ));
        //
        // services.AddScoped<IProcessTimerFactory, NullProcessTimerFactory>();
        
        services.AddScoped<IProcessTimerFactory>(x =>
            new CompositeProcessTimerFactory(
                new LoggingProcessTimerFactory(x.GetService<IBenzeneLogger>())
                // new XRayProcessTimerFactory()
                ));

        // services.AddScoped<IOrderDbClient, OrderDbClient>();
        // services.AddDbContext<DataContext>(x => x.UseNpgsql(configuration["DB_CONNECTION_STRING"],
        //     pgOptions => pgOptions.ProvidePasswordCallback(DbConnectionStringFactory.PasswordCallback())));
        //
        // services.AddScoped<IDataContext>(x => new OrderDataContext(x.GetService<DataContext>()));
    }
}