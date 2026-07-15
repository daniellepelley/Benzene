using Benzene.Abstractions.MessageHandlers;
using Benzene.AspNet.Core;
using Benzene.Core.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.Mesh.PaymentsService.HealthChecks;
using Benzene.Examples.Mesh.PaymentsService.Handlers;
using Benzene.Examples.Mesh.PaymentsService.Model;
using Benzene.HealthChecks;
using Benzene.Http.Routing;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddSingleton<IMessageHandlerDefinition>(_ =>
            MessageHandlerDefinition.CreateInstance("spec", "", typeof(SpecRequest), typeof(RawStringMessage),
                typeof(SpecMessageHandler)));
        services.AddScoped<SpecMessageHandler>();
        services.AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("get", "/spec", "spec"));
        services.AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("get", "/healthcheck", "healthcheck"));

        if (Environment.GetEnvironmentVariable("DEMO_ADD_ENDPOINT") == "true")
        {
            services.AddSingleton<IMessageHandlerDefinition>(_ =>
                MessageHandlerDefinition.CreateInstance("payments:get-refunds", "", typeof(GetPaymentMessage),
                    typeof(RefundDto[]), typeof(GetPaymentRefundsMessageHandler)));
            services.AddScoped<GetPaymentRefundsMessageHandler>();
            services.AddSingleton<IHttpEndpointDefinition>(_ =>
                new HttpEndpointDefinition("get", "/payments/{id}/refunds", "payments:get-refunds"));
        }

        services.UsingBenzene();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseBenzene(benzene => benzene
            .UseHttp(asp => asp
                .UseSpec()
                .UseHealthCheck("healthcheck", new PaymentsGatewayHealthCheck(), new PaymentsDatabaseHealthCheck(), new FraudEngineHealthCheck())
                .UseMessageHandlers()
            )
        );

        app.UseEndpoints(endpoints => { });
    }
}
