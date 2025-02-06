using Benzene.Abstractions.Middleware;
using Benzene.Aws.ApiGateway;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Aws.Sns;
using Benzene.Core.Logging;
using Benzene.Core.MessageHandlers;
using Benzene.Diagnostics.Correlation;
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

            app.UseBenzeneMessage(directMessageApp => directMessageApp
                .UseCorrelationId()
                .UseTimer("direct-message-application")
                .UseLogResult(x => x
                    .WithTopic()
                    .WithTransport()
                    .WithCorrelationId()
                    .WithHeaders("tenantId", "userId", "sender")
                )
                .UseHealthCheck(healthCheckTopic, healthCheckBuilder)
                .UseMessageHandlers(x => x
                    .UseFluentValidation()
                )
            );

            app.UseSns(snsApp => snsApp
                .UseCorrelationId()
                .UseTimer("sns-application")
                .UseHealthCheck(healthCheckTopic, healthCheckBuilder)
                .UseMessageHandlers(x => x
                    .UseFluentValidation()
                )
            );

            app.UseSqs(sqsApp => sqsApp
                .UseCorrelationId()
                .UseTimer("sqs-application")
                .UseHealthCheck(healthCheckTopic, healthCheckBuilder)
                .UseMessageHandlers(x => x
                    .UseFluentValidation()
                )
            );

            app.UseApiGateway(apiGatewayApp => apiGatewayApp
                .UseCorrelationId()
                .UseTimer("api-gateway-pipeline")
                .UseHealthCheck(healthCheckTopic, "POST", "/healthcheck", healthCheckBuilder)
                .UseMessageHandlers(x => x
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
