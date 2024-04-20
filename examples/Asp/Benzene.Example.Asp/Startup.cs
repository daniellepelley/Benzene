using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.AspNet.Core;
using Benzene.Core.Correlation;
using Benzene.Core.Logging;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Diagnostics.Timers;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Logging;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Examples.App.Validators;
using Benzene.FluentValidation;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Benzene.Microsoft.Logging;
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
            .WriteTo.ApplicationInsights(new TelemetryConfiguration("3f72a47f-1aba-4e7a-913e-b3aa3161e6c6"), TelemetryConverter.Traces)
            .CreateLogger();
        
        services.AddLogging();
        services.AddScoped<ILogger, Logger<string>>();
        services.AddControllers();

        services.AddSingleton(Configuration);
        //services.AddStructuredLogging();
        // services.AddCorrelationId();

        // services.AddScoped<IOrderDbClient, OrderDbClient>();
        services.AddScoped<IOrderDbClient, InMemoryOrderDbClient>();
        services.AddScoped<IOrderService, OrderService>();
        // services.AddScoped<IOrderService, HardcodedOrderService>();

        // services.AddDbContext<DataContext>(x => x.UseNpgsql(Configuration["DB_CONNECTION_STRING"],
        //     pgOptions => pgOptions.ProvidePasswordCallback(DbConnectionStringFactory.PasswordCallback())));

        // services.AddScoped<IDataContext>(x => new OrderDataContext(x.GetService<DataContext>()));
        
        services
            .UsingBenzene(x => x
            .AddStructuredLogging()
            .AddMicrosoftLogger()
            .AddCorrelationId()
            .AddHttpMessageHandlers(typeof(CreateOrderMessage).Assembly)
            .AddAspNetMessageHandlers());

        services.AddValidatorsFromAssemblyContaining<GetOrderMessageValidator>();
        services.AddScoped<IProcessTimerFactory, NullProcessTimerFactory>();
        services.AddSingleton<IMiddlewarePipelineBuilder<AspNetContext>>(
            new MiddlewarePipelineBuilder<AspNetContext>(new MicrosoftBenzeneServiceContainer(services)));

        // services.AddScoped<AspNetContextResponseMiddleware>();

        // services.AddScoped<IProcessTimerFactory>(x =>
        // new CompositeProcessTimerFactory(
        // new LoggingProcessTimerFactory(x.GetService<ILogger>())));
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
            .UseCorrelationId()
            .UseLogTopic()
            // .UseLogContext("http")
            .UseProcessResponse()
            .UseMessageRouter(x => x.UseFluentValidation())
        );

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}