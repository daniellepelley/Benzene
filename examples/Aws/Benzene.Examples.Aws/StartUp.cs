using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway.ApiGatewayCustomAuthorizer;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Aws.Lambda.EventBridge;
using Benzene.Aws.Lambda.Kafka;
using Benzene.Aws.Lambda.Sns;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Diagnostics.Correlation;
using Benzene.Diagnostics.Timers;
using Benzene.Examples.Aws.Logging;
using Benzene.FluentValidation;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
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
                .UseXml()
                .UseHealthCheck(healthCheckTopic, healthChecks)
                .UseMessageHandlers(router => router
                    .UseFluentValidation()
                );
        
        app.UseBenzeneMessage(benzeneMessagePipeline);

        app.UseApiGateway(apiGatewayApp => apiGatewayApp
            .UseHttpToBenzeneMessage(benzeneMessagePipeline)
            .UseTimer("api-gateway-application")
            .UseXml()
            .UseHealthCheck("healthcheck", "POST", "/healthcheck", healthChecks)
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        );

        app.UseSns(snsApp => snsApp
            .UseTimer("sns-application")
            .UseXml()
            .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        );

        app.UseSqs(sqsApp => sqsApp
            .UseTimer("sqs-application")
            .UseXml()
            .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        );

        app.UseKafka(kafkaApp => kafkaApp
            .UseTimer("kafka-application")
            .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageHandlers(router => router.UseFluentValidation())
        );

        // EventBridge routes by the event's detail-type; detail is JSON, so no XML mapping needed.
        app.UseEventBridge(eventBridgeApp => eventBridgeApp
            .UseTimer("event-bridge-application")
            .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        );

        app.UseApiGatewayCustomAuthorizer(authorizerApp => authorizerApp
            .UseCustomAuthorizer(request => Task.FromResult(new APIGatewayCustomAuthorizerResponse
            {
                PrincipalID = "user",
                PolicyDocument = new APIGatewayCustomAuthorizerPolicy
                {
                    Version = "2012-10-17",
                    Statement =
                    [
                        new APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement
                        {
                            Effect = "Allow",
                            Action = ["execute-api:Invoke"],
                            Resource = [request.MethodArn]
                        }
                    ]
                }
            }))
        );
    }
}