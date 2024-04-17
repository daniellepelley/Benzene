﻿using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Aws.ApiGateway;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Aws.Core.DirectMessage;
using Benzene.Aws.Sns;
using Benzene.Aws.Sqs;
using Benzene.Core.Correlation;
using Benzene.Core.DirectMessage;
using Benzene.Core.Logging;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Core.Response;
using Benzene.Diagnostics.Timers;
using Benzene.FluentValidation;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Test.Elements.Examples
{
    public class LambdaEntryPoint : AwsLambdaStartUp
    {
        public override void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IConfiguration configuration)
        {
            app.UseLogResult(x => x
                    .WithApplication()
                    .WithRequestId()
                )
                // .HandleExceptions()
                .UseTimer("aws-stream-application");

            var healthCheckBuilder = app.GetHealthCheckerBuilder()
                .AddHealthCheck(new SimpleHealthCheck());

            const string healthCheckTopic = "hello:world:healthcheck";

            app.UseDirectMessage(directMessageApp => directMessageApp
                .UseCorrelationId()
                .UseTimer("direct-message-application")
                .UseLogResult(x => x
                    .WithTopic()
                    .WithTransport()
                    .WithCorrelationId()
                    .WithHeaders("tenantId", "userId", "sender")
                )
                .UseProcessResponse()
                .UseHealthCheck(healthCheckTopic, healthCheckBuilder)
                .UseMessageRouter(x => x
                    .UseFluentValidation()
                )
            );

            app.UseSns(snsApp => snsApp
                .UseCorrelationId()
                .UseTimer("sns-application")
                .UseProcessResponse()
                .UseHealthCheck(healthCheckTopic, healthCheckBuilder)
                .UseMessageRouter(x => x
                    .UseFluentValidation()
                )
            );

            app.UseSqs(sqsApp => sqsApp
                .UseCorrelationId()
                .UseTimer("sqs-application")
                .UseProcessResponse()
                .UseHealthCheck(healthCheckTopic, healthCheckBuilder)
                .UseMessageRouter(x => x
                    .UseFluentValidation()
                )
            );

            app.UseApiGateway(apiGatewayApp => apiGatewayApp
                .UseCorrelationId()
                .UseTimer("api-gateway-pipeline")
                .UseProcessResponse()
                .UseHealthCheck(healthCheckTopic, "POST", "/healthcheck", healthCheckBuilder)
                .UseMessageRouter(x => x
                    .UseFluentValidation()
                )
            );
        }

        public override IConfiguration GetConfiguration()
        {
            return DependenciesBuilder.GetConfiguration();
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            DependenciesBuilder.Register(services, configuration);
        }
    }
}
