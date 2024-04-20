using System.IO;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Aws.Core;
using Benzene.Aws.Core.ApiGateway;
using Benzene.Aws.Core.ApiGatewayCustomAuthorizer;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Aws.Core.BenzeneMessage;
using Benzene.Aws.Core.DirectMessage;
using Benzene.Aws.Core.Kafka;
using Benzene.Aws.Core.Sns;
using Benzene.Aws.Core.Sqs;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Correlation;
using Benzene.Core.Logging;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Diagnostics.Timers;
using Benzene.FluentValidation;
using Benzene.HealthChecks;
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
            .AddXml()
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
                .UseLogContext()
                .UseLogResult()
                .UseProcessResponse()
                .UseHealthCheck(healthCheckTopic, healthChecks)
                .UseMessageRouter(router => router
                    .UseFluentValidation()
                );
        
        app.UseDirectMessage(benzeneMessagePipeline);

        app.UseApiGateway(apiGatewayApp => apiGatewayApp
            // .Use<ApiGatewayContext, Extensions.AdminBenzeneMessageMiddleware>()
            .UseHttpToBenzeneMessage(benzeneMessagePipeline)
            .UseCorrelationId()
            .UseTimer("api-gateway-application")
            .UseLogContext()
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