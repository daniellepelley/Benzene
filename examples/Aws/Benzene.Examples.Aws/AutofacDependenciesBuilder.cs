using System.IO;
using Amazon;
using Amazon.SQS;
using FluentValidation;
using Benzene.Aws.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Amazon.Extensions.NETCore.Setup;
using Autofac;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Response;
using Benzene.Autofac;
using Benzene.Aws.ApiGateway;
using Benzene.Aws.Sns;
using Benzene.Aws.Sqs;
using Benzene.Aws.XRay;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.DI;
using Benzene.Core.Request;
using Benzene.Core.Response;
using Benzene.Diagnostics;
using Benzene.Diagnostics.Timers;
using Benzene.Examples.App.Custom;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Logging;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Examples.App.Validators;
using Benzene.Http;
using Benzene.Microsoft.Logging;
using Benzene.Serilog.Logging;
using Benzene.Xml;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using JsonSerializer = Benzene.Core.Serialization.JsonSerializer;

namespace Benzene.Examples.Aws;

public static class AutofacDependenciesBuilder
{
    public static void Register(ContainerBuilder containerBuilder, IConfiguration configuration)
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

        containerBuilder.RegisterInstance(configuration).SingleInstance();
        // services.AddLogging(x => x.AddConsole());

        containerBuilder.RegisterInstance(new LoggerFactory())
            .As<ILoggerFactory>();

        containerBuilder.RegisterInstance(NullLoggerProvider.Instance)
            .As<ILoggerProvider>();

        containerBuilder.RegisterGeneric(typeof(Logger<>))
            .As(typeof(ILogger<>))
            .SingleInstance();

        containerBuilder.RegisterType<Logger<string>>().As<ILogger>().InstancePerLifetimeScope();
        containerBuilder.Register(_ => configuration.GetAWSOptions()).InstancePerLifetimeScope();
        containerBuilder.RegisterInstance(awsOptions.CreateServiceClient<IAmazonSQS>()).SingleInstance();
        containerBuilder.RegisterType<CreateOrderMessageValidator>().As<IValidator<CreateOrderMessage>>()
            .SingleInstance();

        containerBuilder.UsingBenzene(x => x
                .AddBenzene()
                .AddStructuredLogging()
            .AddMicrosoftLogger()
            .AddXml()
            .AddMessageHandlers(typeof(CreateOrderMessage).Assembly)
            .AddScoped<IOrderDbClient, InMemoryOrderDbClient>()
            .AddScoped<IOrderService, OrderService>()
            .AddScoped<ResponseMiddleware<ApiGatewayContext>>()
            .AddScoped<IResponseHandler<ApiGatewayContext>,
                ResponseHandler<JsonSerializationResponseHandler<ApiGatewayContext>, ApiGatewayContext>>()
            .AddScoped<IResponseHandler<ApiGatewayContext>,
                ResponseHandler<XmlSerializationResponseHandler<ApiGatewayContext>, ApiGatewayContext>>()
            .AddScoped<JsonSerializationResponseHandler<ApiGatewayContext>>()
            .AddScoped<XmlSerializationResponseHandler<ApiGatewayContext>>()
            .AddScoped<IResponseHandler<ApiGatewayContext>, HttpStatusCodeResponseHandler<ApiGatewayContext>>()
            .AddScoped<IRequestEnricher<ApiGatewayContext>, ApiGatewayRequestEnricher>()
            // .AddScoped<IMiddlewareFactory>(_ => new TimerMiddlewareFactory(
            //     new XRayProcessTimerFactory()))
            // .AddScoped<IProcessTimerFactory, NullProcessTimerFactory>()
            .AddScoped<IResponsePayloadMapper<ApiGatewayContext>, CustomResponsePayloadMapper<ApiGatewayContext>>()
            .AddScoped<IResponsePayloadMapper<BenzeneMessageContext>,
                CustomResponsePayloadMapper<BenzeneMessageContext>>()
            .AddScoped<IResponsePayloadMapper<SqsMessageContext>, CustomResponsePayloadMapper<SqsMessageContext>>()
            .AddScoped<IResponsePayloadMapper<SnsRecordContext>, CustomResponsePayloadMapper<SnsRecordContext>>()
            .AddSingleton<ISerializerOption<BenzeneMessageContext>>(
                new SerializerOption<BenzeneMessageContext, JsonSerializer>(x => x.Always()))
            // .AddSingleton<ISerializerOption<ApiGatewayContext>, XmlApiGatewaySerializerOption>()
            .AddSingleton<ISerializerOption<ApiGatewayContext>>(
                new SerializerOption<ApiGatewayContext, JsonSerializer>(x => x.Always()))
            // .AddSingleton<ISerializerOption<SnsRecordContext>, XmlSnsSerializerOption>()
            .AddSingleton<ISerializerOption<SnsRecordContext>>(
                new SerializerOption<SnsRecordContext, JsonSerializer>(x => x.Always()))
            // .AddSingleton<ISerializerOption<SqsMessageContext>, XmlSerializerOption<SqsMessageContext>>()
            .AddSingleton<ISerializerOption<SqsMessageContext>>(
                new SerializerOption<SqsMessageContext, JsonSerializer>(x => x.Always()))
            .AddSingleton<JsonSerializer>()
        // .AddSingleton<XmlSerializer>()
        );


        // services.AddScoped<IProcessTimerFactory>(x =>
        //     new CompositeProcessTimerFactory(
        //         new LoggingProcessTimerFactory(x.GetService<ILogger>()),
        //         new XRayProcessTimerFactory()));
    }
}