﻿using System.IO;
using Autofac;
using Benzene.Diagnostics.Timers;
using Benzene.HealthChecks;
using Microsoft.Extensions.Configuration;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway.ApiGatewayCustomAuthorizer;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Aws.Lambda.Kafka;
using Benzene.Aws.Lambda.Sns;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Aws.XRay;
using Benzene.Core.MessageHandlers;
using Benzene.Diagnostics.Correlation;
using Benzene.Examples.Aws.Logging;
using Benzene.FluentValidation;
using Benzene.HealthChecks.Core;

namespace Benzene.Examples.Aws;

public class StartUpAutofac : AutofacAwsStartUp, IStartUp<ContainerBuilder, IConfiguration, IMiddlewarePipelineBuilder<AwsEventStreamContext>>{
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
            // .UseLogContext()
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
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        );

        app.UseBenzeneMessage(benzeneMessageApp => benzeneMessageApp
            .UseTimer("benzene-message-application")
            .UseCorrelationId()
            // .UseLogContext()
            .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        );

        app.UseSns(snsApp => snsApp
            .UseCorrelationId()
            .UseTimer<SnsRecordContext>("sns-application")
            // .UseTestLogger()
            // .UseLogContext()
            // .UseBroadcastResult()
            // .UseProcessResponseSns()
            .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        );

        app.UseSqs(sqsApp => sqsApp
            .UseCorrelationId()
            .UseTimer<SqsMessageContext>("sqs-application")
            // .UseTestLogger()
            // .UseLogContext()
            // .UseBroadcastResult()
            // .UseProcessResponseSqs()
            .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        );

        app.UseKafka(kafkaApp => kafkaApp
            .UseCorrelationId()
            .UseTimer("kafka-application")
            // .UseLogContext()
            // .UseLogResult()
            // .UseProcessResponseSqs()
            // .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageHandlers(router => router.UseFluentValidation()
            )
        );

        app.UseApiGatewayCustomAuthorizer(authorizerApp => authorizerApp
            .UseMessageHandlers()
        );
    }
}