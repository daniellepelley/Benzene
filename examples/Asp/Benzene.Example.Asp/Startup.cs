using Benzene.AspNet.Core;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Logging;
using Benzene.Examples.App.Services;
using Benzene.Examples.App.Validators;
using Benzene.Microsoft.Dependencies;
using FluentValidation;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Core.Middleware;
using Benzene.Diagnostics.Correlation;
using Benzene.FluentValidation;
using Benzene.Http.Routing;
using Benzene.Schema.OpenApi;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Benzene.Example.Asp;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(new CustomJsonFormatter())
            .WriteTo.ApplicationInsights(new TelemetryConfiguration("3f72a47f-1aba-4e7a-913e-b3aa3161e6c6"),
                TelemetryConverter.Traces)
            .CreateLogger();

        services.AddLogging();
        services.AddScoped<ILogger, Logger<string>>();
        services.AddControllers();

        services.AddSingleton(Configuration);

        services.AddScoped<IOrderDbClient, InMemoryOrderDbClient>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddSingleton<IMessageHandlerDefinition>(_ =>
            MessageHandlerDefinition.CreateInstance("spec", "", typeof(SpecRequest), typeof(RawStringMessage),
                typeof(SpecMessageHandler)));
        services.AddScoped<SpecMessageHandler>();
        services.AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("get", "/spec", "spec"));

        services.UsingBenzene();

        services.AddValidatorsFromAssemblyContaining<GetOrderMessageValidator>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.UseBenzene(benzene => benzene
            .UseAspNet(asp => asp
                .UseCorrelationId()
                .UseSpec()
                // .UseCors(new CorsSettings
                // {
                    // AllowedDomains = ["https://editor-next.swagger.io"],
                    // AllowedHeaders = Array.Empty<string>()
                // })
                .UseMessageHandlers(x => x.UseFluentValidation())
            )
        );

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}