using System.IO;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.ApiGateway;
using Benzene.Aws.ApiGateway.ApiGatewayCustomAuthorizer;
using Benzene.Aws.Core;
using Benzene.Aws.ApiGateway;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Aws.Core.BenzeneMessage;
using Benzene.Aws.Kafka;
using Benzene.Aws.Sns;
using Benzene.Aws.Sqs;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Correlation;
using Benzene.Core.Logging;
using Benzene.Core.MessageHandling;
using Benzene.Diagnostics.Timers;
using Benzene.FluentValidation;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Serilog.Logging;
using Benzene.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                .UseLogResult()
                .UseProcessResponse()
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
            .UseProcessResponse()
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
            .UseLogResult()
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
            .UseLogResult()
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