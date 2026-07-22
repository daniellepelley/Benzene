using Benzene.HostedService;
using BenzeneStarter;

// A plain .NET generic host - no Azure Functions/broker runtime. Benzene owns the process and hosts
// the RabbitMQ consumer as a background IHostedService (see StartUp.Configure).
IHost host = Host.CreateDefaultBuilder(args)
    .UseBenzene<StartUp>()
    .Build();

await host.RunAsync();
