using System.IO;
using Autofac;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Aws.Core;
using Benzene.Aws.Core.ApiGateway;
using Benzene.Aws.Core.ApiGatewayCustomAuthorizer;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Aws.Core.BenzeneMessage;
using Benzene.Aws.Core.Kafka;
using Benzene.Aws.Core.Sns;
using Benzene.Aws.Core.Sqs;
using Benzene.Core.Correlation;
using Benzene.Core.Logging;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Diagnostics.Timers;
using Benzene.FluentValidation;
using Benzene.HealthChecks;
using Benzene.Serilog.Logging;
using Microsoft.Extensions.Configuration;

namespace Benzene.Examples.Aws;

public class StartUpAutofac : AutofacAwsStartUp, IAwsStartUp<ContainerBuilder, AwsEventStreamContext>
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            // .AddJsonFile("config.json")
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(ContainerBuilder services, IConfiguration configuration)
    {
        AutofacDependenciesBuilder.Register(services, configuration);
    }

    public override void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IConfiguration configuration)
    {
        app
            .UseSerilog()
            .UseXRayTracing(true)
            // .UseLogApplication()
            .UseTimer("aws-stream-application");

        var healthChecks = new IHealthCheck[]
        {
            new SimpleHealthCheck()
            // new MessageSchemaHealthCheck(),
            // new DatabaseHealthCheck<DataContext>("20220809094008_V8"),
        };

        const string healthCheckTopic = "healthcheck";

        app.UseApiGateway(apiGatewayApp => apiGatewayApp
            // .Use<ApiGatewayContext, AdminBenzeneMessageMiddleware>()
            .UseCorrelationId()
            .UseTimer("api-gateway-application")
            .UseLogContext()
            .UseHealthCheck("healthcheck", "POST", "/healthcheck", new SimpleHealthCheck())
            // .AsHttp(http => http.UseRequestMapping(x => x
            //         .Use<XmlHttpMessageBodyMapperOption>()
            //         .UseDefault<JsonHttpMessageBodyMapper>()
            //     // .Use(new MessageBodyMapperOption<ApiGatewayContext, JsonApiGatewayMessageBodyMapper>(context => true))
            // ))
            // .AsBenzeneMessage(x => x.UseRequestMapping(x => x
            //         .Use<XmlBenzeneMessageBodyMapperOption>()
            //         .UseDefault<JsonBenzeneMessageBodyMapper>()
            //     // .Use(new MessageBodyMapperOption<ApiGatewayContext, JsonApiGatewayMessageBodyMapper>(context => true))
            // ))
            // .UseBroadcastResult()
            .UseProcessResponse()
            .UseMessageRouter(router => router
                .UseFluentValidation()
            )
        );

        app.UseBenzeneMessage(benzeneMessageApp => benzeneMessageApp
            .UseTimer("benzene-message-application")
            .UseCorrelationId()
            .UseLogContext()
            .UseLogResult()
            .UseProcessResponse()
            .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageRouter(router => router
                .UseFluentValidation()
            )
        );

        app.UseSns(snsApp => snsApp
            .UseCorrelationId()
            .UseTimer<SnsRecordContext>("sns-application")
            // .UseTestLogger()
            .UseLogContext()
            .UseLogResult()
            // .UseBroadcastResult()
            // .UseProcessResponseSns()
            .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageRouter(router => router
                .UseFluentValidation()
            )
        );

        app.UseSqs(sqsApp => sqsApp
            .UseCorrelationId()
            .UseTimer<SqsMessageContext>("sqs-application")
            // .UseTestLogger()
            .UseLogContext()
            .UseLogResult()
            // .UseBroadcastResult()
            // .UseProcessResponseSqs()
            .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageRouter(router => router
                .UseFluentValidation()
            )
        );

        app.UseKafka(kafkaApp => kafkaApp
            .UseCorrelationId()
            .UseTimer("kafka-application")
            .UseLogContext()
            // .UseLogResult()
            // .UseProcessResponseSqs()
            // .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageRouter(router => router.UseFluentValidation()
            )
        );

        app.UseApiGatewayCustomAuthorizer(authorizerApp => authorizerApp
            .UseMessageRouter<ApiGatewayCustomAuthorizerContext>()
        );
    }
}