using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
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
            .UseTimer("aws-stream-application");

        var healthChecks = new IHealthCheck[]
        {
            new SimpleHealthCheck()
        };

        const string healthCheckTopic = "healthcheck";

        app.UseApiGateway(apiGatewayApp => apiGatewayApp
            .UseTimer("api-gateway-application")
            .UseHealthCheck("healthcheck", "POST", "/healthcheck", new SimpleHealthCheck())
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        );

        app.UseBenzeneMessage(benzeneMessageApp => benzeneMessageApp
            .UseTimer("benzene-message-application")
            .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        );

        app.UseSns(snsApp => snsApp
            .UseTimer<SnsRecordContext>("sns-application")
            .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        );

        app.UseSqs(sqsApp => sqsApp
            .UseTimer<SqsMessageContext>("sqs-application")
            .UseHealthCheck(healthCheckTopic, healthChecks)
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        );

        app.UseKafka(kafkaApp => kafkaApp
            .UseTimer("kafka-application")
            .UseMessageHandlers(router => router.UseFluentValidation()
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