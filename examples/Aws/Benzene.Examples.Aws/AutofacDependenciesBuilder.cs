using Amazon;
using Amazon.SQS;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Amazon.Extensions.NETCore.Setup;
using Autofac;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Autofac;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Sns;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Examples.App.Validators;
using Benzene.Http;
using Benzene.Xml;
using Newtonsoft.Json;

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

        containerBuilder.RegisterInstance(LoggerFactory.Create(x => x.AddConsole()))
            .As<ILoggerFactory>();

        containerBuilder.Register(_ => configuration.GetAWSOptions()).InstancePerLifetimeScope();
        containerBuilder.RegisterInstance(awsOptions.CreateServiceClient<IAmazonSQS>()).SingleInstance();
        containerBuilder.RegisterType<CreateOrderMessageValidator>().As<IValidator<CreateOrderMessage>>()
            .SingleInstance();

        containerBuilder.UsingBenzene(x => x
            .AddBenzene()
            .AddXml()
            .AddMediaFormatNegotiation<ApiGatewayContext>()
            .AddMessageHandlers(typeof(CreateOrderMessage).Assembly)
            .AddScoped<IOrderDbClient, InMemoryOrderDbClient>()
            .AddScoped<IOrderService, OrderService>()
            // .AddScoped<ResponseMiddleware<ApiGatewayContext>>()
            .AddScoped<IResponseRenderer<ApiGatewayContext>, SerializerResponseRenderer<ApiGatewayContext>>()
            .AddScoped<IResponseHandler<ApiGatewayContext>, RendererResponseHandler<ApiGatewayContext>>()
            .AddScoped<IResponseHandler<ApiGatewayContext>, HttpStatusCodeResponseHandler<ApiGatewayContext>>()
            .AddScoped<IRequestEnricher<ApiGatewayContext>, ApiGatewayRequestEnricher>()
            // .AddScoped<IMiddlewareFactory>(_ => new TimerMiddlewareFactory(
            //     new XRayProcessTimerFactory()))
            // .AddScoped<IProcessTimerFactory, NullProcessTimerFactory>()
            // .AddScoped<IResponsePayloadMapper<ApiGatewayContext>, CustomResponsePayloadMapper<ApiGatewayContext>>()
            // .AddScoped<IResponsePayloadMapper<BenzeneMessageContext>,
                // CustomResponsePayloadMapper<BenzeneMessageContext>>()
            // .AddScoped<IResponsePayloadMapper<SqsMessageContext>, CustomResponsePayloadMapper<SqsMessageContext>>()
            // .AddScoped<IResponsePayloadMapper<SnsRecordContext>, CustomResponsePayloadMapper<SnsRecordContext>>()
            // .AddSingleton<ISerializerOption<BenzeneMessageContext>>(
                // new SerializerOption<BenzeneMessageContext, JsonSerializer>(x => x.Always()))
            // .AddSingleton<ISerializerOption<ApiGatewayContext>, XmlApiGatewaySerializerOption>()
            // .AddSingleton<ISerializerOption<ApiGatewayContext>>(
                // new SerializerOption<ApiGatewayContext, JsonSerializer>(x => x.Always()))
            // .AddSingleton<ISerializerOption<SnsRecordContext>, XmlSnsSerializerOption>()
            // .AddSingleton<ISerializerOption<SnsRecordContext>>(
                // new SerializerOption<SnsRecordContext, JsonSerializer>(x => x.Always()))
            // .AddSingleton<ISerializerOption<SqsMessageContext>, XmlSerializerOption<SqsMessageContext>>()
            // .AddSingleton<ISerializerOption<SqsMessageContext>>(
                // new SerializerOption<SqsMessageContext, JsonSerializer>(x => x.Always()))
            .AddSingleton<JsonSerializer>()
        // .AddSingleton<XmlSerializer>()
        );


        // services.AddScoped<IProcessTimerFactory>(x =>
        //     new CompositeProcessTimerFactory(
        //         new LoggingProcessTimerFactory(x.GetService<ILogger>()),
        //         new XRayProcessTimerFactory()));
    }
}