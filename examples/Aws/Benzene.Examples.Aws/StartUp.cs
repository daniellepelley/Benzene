using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway.ApiGatewayCustomAuthorizer;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Aws.Lambda.EventBridge;
using Benzene.Aws.Lambda.Kafka;
using Benzene.Aws.Lambda.Sns;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Diagnostics.Timers;
using Benzene.Examples.Aws.Logging;
using Benzene.FluentValidation;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Http.BenzeneMessage;
using Benzene.Microsoft.Dependencies;
using Benzene.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.Aws;

/// <summary>
/// Platform-neutral application definition, hosted as an AWS Lambda entry point by
/// <see cref="Function"/>. All AWS event sources are wired inside <c>app.UseAwsLambda(...)</c>.
/// </summary>
public class StartUp : BenzeneStartUp
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

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseAwsLambda(aws =>
        {
            aws
                .UseSerilog()
                .UseTimer("aws-stream-application");

            var healthChecks = new IHealthCheck[]
            {
                new SimpleHealthCheck()
            };

            const string healthCheckTopic = "healthcheck";

            var benzeneMessagePipeline =
                aws.Create<BenzeneMessageContext>()
                    .UseTimer("benzene-message-application")
                    .UseXml()
                    .UseHealthCheck(healthCheckTopic, healthChecks)
                    .UseMessageHandlers(router => router
                        .UseFluentValidation()
                    );

            aws.UseBenzeneMessage(benzeneMessagePipeline);

            aws.UseApiGateway(apiGatewayApp => apiGatewayApp
                .UseBenzeneMessage(benzeneMessagePipeline)
                .UseTimer("api-gateway-application")
                .UseXml()
                .UseHealthCheck("healthcheck", "POST", "/healthcheck", healthChecks)
                .UseMessageHandlers(router => router
                    .UseFluentValidation()
                )
            );

            aws.UseSns(snsApp => snsApp
                .UseTimer("sns-application")
                .UseXml()
                .UseHealthCheck(healthCheckTopic, healthChecks)
                .UseMessageHandlers(router => router
                    .UseFluentValidation()
                )
            );

            aws.UseSqs(sqsApp => sqsApp
                .UseTimer("sqs-application")
                .UseXml()
                .UseHealthCheck(healthCheckTopic, healthChecks)
                .UseMessageHandlers(router => router
                    .UseFluentValidation()
                )
            );

            aws.UseKafka(kafkaApp => kafkaApp
                .UseTimer("kafka-application")
                .UseHealthCheck(healthCheckTopic, healthChecks)
                .UseMessageHandlers(router => router.UseFluentValidation())
            );

            // EventBridge routes by the event's detail-type; detail is JSON, so no XML mapping needed.
            aws.UseEventBridge(eventBridgeApp => eventBridgeApp
                .UseTimer("event-bridge-application")
                .UseHealthCheck(healthCheckTopic, healthChecks)
                .UseMessageHandlers(router => router
                    .UseFluentValidation()
                )
            );

            aws.UseApiGatewayCustomAuthorizer(authorizerApp => authorizerApp
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
        });
    }
}

/// <summary>
/// AWS Lambda entry point hosting <see cref="StartUp"/>. Point the function-handler setting at
/// <c>Benzene.Examples.Aws::Benzene.Examples.Aws.Function::FunctionHandlerAsync</c>.
/// </summary>
public class Function : AwsLambdaHost<StartUp>;
