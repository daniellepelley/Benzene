using Benzene.Example.Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    // .ConfigureAppConfiguration(config =>
    // {
    //     config.SetBasePath(Directory.GetCurrentDirectory())
    //         .AddEnvironmentVariables();
    // })
    // .ConfigureServices(services =>
    // {
    //     services.AddApplicationInsightsTelemetryWorkerService();
    //     services.ConfigureFunctionsApplicationInsights();
    // })
    .ConfigureWebJobs((context, builder) => new StartUp().Configure(builder))
    .Build();

host.Run();
