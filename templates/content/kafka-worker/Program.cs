using Benzene.HostedService;
using BenzeneStarter;

IHost host = Host.CreateDefaultBuilder(args)
    .UseBenzene<StartUp>()
    .Build();

await host.RunAsync();
