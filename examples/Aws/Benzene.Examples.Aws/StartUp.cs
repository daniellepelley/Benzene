using System.IO;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.ApiGateway;
using Benzene.Aws.ApiGateway.ApiGatewayCustomAuthorizer;
using Benzene.Aws.Kafka;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Aws.Sns;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Diagnostics.Correlation;
using Benzene.Diagnostics.Timers;
using Benzene.FluentValidation;
using Benzene.HealthChecks;
using Benzene.Serilog.Logging;
using Benzene.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IHealthCheck = Benzene.HealthChecks.Core.IHealthCheck;

namespace Benzene.Examples.Aws;

public class StartUp : AwsLambdaStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        DependenciesBuilder.Register(services, configuration);
    }

    public override void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IConfiguration configuration)
    {
        app
            .UseSerilog()
            .UseTimer("aws-stream-application");

        var healthChecks = new IHealthCheck[]
        {
            new SimpleHealthCheck()
        };

        const string healthCheckTopic = "healthcheck";
        
        var benzeneMessagePipeline =
            app.Create<BenzeneMessageContext>()
                .UseTimer("benzene-message-application")
                .UseCorrelationId()
                // .UseLogContext()
                // .UseLogResult()
                .UseXml()
                .UseHealthCheck(healthCheckTopic, healthChecks)
                .UseMessageHandlers(router => router
                    .UseFluentValidation()
                );
        
        app.UseBenzeneMessage(benzeneMessagePipeline);

        app.UseApiGateway(apiGatewayApp => apiGatewayApp
            // .Use<ApiGatewayContext, Extensions.AdminBenzeneMessageMiddleware>()
            .UseHttpToBenzeneMessage(benzeneMessagePipeline)
            .UseCorrelationId()
            .UseTimer("api-gateway-application")
            // .UseLogContext()
            .UseXml()
            .UseHealthCheck("healthcheck", "POST", "/healthcheck", healthChecks)
            // .UseSerializer(x => x.Use<XmlSerializer>())
            // .AsHttp(http => http.UseRequestMapping(x => x:w
            //         .Use<XmlHttpMessageBodyMapperOption>()
            //         .UseDefault<JsonHttpMessageBodyMapper>()
            //     // .Use(new MessageBodyMapperOption<ApiGatewayContext, JsonApiGatewayMessageBodyMapper>(context => true))
            // ))
            // .AsBenzeneMessage(x => x.UseRequestMapping(x => x
            //         .Use<XmlBenzeneMessageBodyMapperOption>()
            //         .UseDefault<JsonBenzeneMessageBodyMapper>()
            //     // .Use(new MessageBodyMapperOption<ApiGatewayContext, JsonApiGatewayMessageBodyMapper>(context => true))
            // ))
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        );


        app.UseSns(snsApp => snsApp
            .UseCorrelationId()
            .UseTimer<SnsRecordContext>("sns-application")
            // .UseTestLogger()
            // .UseLogContext()
            // .UseLogResult()
            // .UseBroadcastResult()
            // .UseProcessResponseSns()
            .UseXml()
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
            // .UseLogResult()
            // .UseBroadcastResult()
            // .UseProcessResponseSqs()
            .UseXml()
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
            .UseMessageHandlers<ApiGatewayCustomAuthorizerContext>()
        );
    }
}